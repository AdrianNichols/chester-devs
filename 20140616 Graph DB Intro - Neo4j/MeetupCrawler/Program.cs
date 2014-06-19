using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ScrapySharp.Network;
using ScrapySharp.Extensions;
using Neo4jClient;

namespace MeetupCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Neo db = new Neo();
            ScrapingBrowser browser = new ScrapingBrowser();
            List<HtmlNode> memberLinks = new List<HtmlNode>();

            string startPoint = "http://www.meetup.com/Chester-Devs/members/?offset={0}&desc=1&sort=social_sort";
            for(int pages = 1; pages <= 12; pages++)
            {
                WebPage memberList = browser.NavigateToPage(new Uri(String.Format(startPoint, ((pages -1) * 20).ToString())));
                memberLinks.AddRange(memberList.Html.CssSelect("a.memName").ToList());
            }

            List<Person> people = new List<Person>();
            people.AddRange(memberLinks.Select<HtmlNode, Person>(ml => new Person() { Name = ml.InnerText, LinkToProfile = ml.Attributes["href"].Value }).ToList());

            foreach (Person person in people)
            {
                WebPage memberPage = browser.NavigateToPage(new Uri(person.LinkToProfile));
                db.Create<Person>(person);

                Location location = new Location() { Name = memberPage.Html.CssSelect("span.locality").Select<HtmlNode, string>(l => l.InnerText).First() };
                db.Create<Location>(location);
                db.RelatePersonToLocation(person.UniqueId, location.Name);

                List<Interest> interests = new List<Interest>();
                interests.AddRange(memberPage.Html.CssSelect("ul#memberTopicList > li.D_group > div > a.topic-widget").Select<HtmlNode, Interest>(i => new Interest() { Name = i.InnerText }).ToList());
                interests.ForEach(i => db.Create<Interest>(i));
                interests.ForEach(i => db.RelatePersonToInterest(person.UniqueId, i.Name));

                List<Meetup> meetups = new List<Meetup>();
                meetups.AddRange(memberPage.Html.CssSelect("div.D_name").Select<HtmlNode, Meetup>(g => new Meetup() { Name = g.InnerText }).ToList());

                if (meetups.Where(g => g.Name == "Chester Devs").Count() == 0)
                    meetups.Add(new Meetup() { Name = "Chester Devs" });

                meetups.ForEach(g => db.Create<Meetup>(g));
                meetups.ForEach(g => db.RelatePersonToMeetup(person.UniqueId, g.Name));

            }

        }
    }

    public class Neo
    {
        public Neo () {
            _client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            _client.Connect();
        }

        private GraphClient _client;

        public void Create<T>(T entity)
        {
            _client.Cypher
                .Merge(string.Concat("(", typeof(T).Name.ToLowerInvariant(), ":", typeof(T).Name, " { Name : { name } })"))
                .Set(string.Concat(typeof(T).Name.ToLowerInvariant(), " = {entity}"))
                .WithParam("name", entity.GetType().GetProperty("Name").GetValue(entity).ToString())
                .WithParam("entity", entity)
                .ExecuteWithoutResults();
        }

        public void RelatePersonToLocation(string personId, string locationName)
        {
            _client.Cypher
                .Match("(p:Person), (l:Location)")
                .Where((Person p) => p.UniqueId == personId)
                .AndWhere((Location l) => l.Name == locationName)
                .Create("p-[:LOCATED_IN]->l")
                .ExecuteWithoutResults();
        }


        public void RelatePersonToMeetup(string personId, string meetupName)
        {
            _client.Cypher
                .Match("(p:Person), (m:Meetup)")
                .Where((Person p) => p.UniqueId == personId)
                .AndWhere((Meetup m) => m.Name == meetupName)
                .Create("p-[:MEMBER_OF]->m")
                .ExecuteWithoutResults();

        }


        public void RelatePersonToInterest(string personId, string interestName)
        {
            _client.Cypher
                .Match("(p:Person), (i:Interest)")
                .Where((Person p) => p.UniqueId == personId)
                .AndWhere((Interest i) => i.Name == interestName)
                .Create("p-[:INTERESTED_IN]->i")
                .ExecuteWithoutResults();
        }


    }

    public class BaseEntity
    {
        public string UniqueId { get; set; }
        public string Name { get; set; }

        public BaseEntity()
        {
            UniqueId = Guid.NewGuid().ToString();
        }
    }

    public class Meetup : BaseEntity
    {
    }

    public class Location : BaseEntity
    {
    }

    public class Interest : BaseEntity
    {
    }

    public class Person : BaseEntity
    {
        public string Name { get; set; }
        public string LinkToProfile { get; set; }
    }
}
