# docker build

docker build -t danielfoosball/foosballapi:1 .

# docker run

docker run -p 8080:80 danielfoosball/foosballapi:1

# list docker images

docker images

# obs

you cant access swagger html page from docker, beacause its in production and swagger only works in development

# stop docker run

just kill the terminal in vscode should work

# generate schema for database

https://support.sqldbm.com/en/knowledge-bases/2/articles/2407-how-to-generate-sql-script-from-pgadmin-for-reverse-engineering
