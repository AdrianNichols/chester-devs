function AjaxApi() {}

AjaxApi.prototype.ExecuteCypher = function (cypher, onSuccess)
{
    $.ajax({
        type: "POST",
        url: 'http://localhost:7474/db/data/cypher',
        data:  { 'query' : cypher },
        success: function (response) {
            onSuccess(response);
        }
    });


}