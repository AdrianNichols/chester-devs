var catSetup = '' +
    'CREATE (:ProductCategory { ID:  1, Name: "Doodads" }) ' +
    ' CREATE (:ProductCategory { ID:  1, Name: "Thingys" }) ' +
    'CREATE (c:ProductCategory { ID:  1, Name: "Whasats" })';


var recProds = 'MATCH (p:Product) RETURN p';

var recQuery = function(prods) {
    return 'MATCH (p:Product)<-[:BOUGHT]-(c) ' +
    'WHERE p.ID IN ' + prods +
    'OPTIONAL MATCH (op)<-[:BOUGHT]-(oc)-[:BOUGHT]->(p)' +
    'WHERE Not(op.ID IN ' + prods + ')' +
    'RETURN op, COUNT(DISTINCT oc.ID) ' +
    'ORDER BY  COUNT(DISTINCT oc.ID) DESC';
}

var recDiscount = function(prods) {
    return 'MATCH (p:Product)<-[:INCLUDES]-(pr:Promotion)-[:INCLUDES]->(pp) ' +
    'WHERE p.ID IN ' + prods + ' ' +
    'AND NOT(pp.ID IN ' + prods + ') ' +
    'WITH COLLECT(DISTINCT pp) AS promos, pr, COUNT(DISTINCT pp.ID) AS prods ' +
    'RETURN CASE WHEN prods = 1 THEN promos ELSE null END AS prod';
}