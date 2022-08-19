# Foosball Api V2 ⚽

`Foosball Api`, A REST api written in C# `.NET CORE 6 WEB API 3.1` and uses `Postgresql` database.

We use `Dapper` for our ORM.

[![forthebadge](http://forthebadge.com/images/badges/built-with-love.svg)](http://forthebadge.com) [![forthebadge](https://forthebadge.com/images/badges/made-with-c-sharp.svg)](http://forthebadge.com) [![forthebadge](http://forthebadge.com/images/badges/makes-people-smile.svg)](http://forthebadge.com)

</div>

# Development

Run the project with the following command

```sh
dotnet run
```

The project run on port `5297` --> `https://localhost:5297/swagger/index.html`

The project runs on docker on port `8080`

## Env variables

Secrets are added to `secrets.json` file with `dotnet user-secrets`

To run the project. The following variables are needed

```json
{
  "SmtpUser": "",
  "SmtpPort": "",
  "SmtpPass": "",
  "SmtpHost": "",
  "SmtpEmailFrom": "",
  "JwtSecret": "",
  "DatoCmsBearer": "",
  "FoosballDbDev": "",
  "FoosballDbProd": ""
}
```

## Technology

[.net core](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-5.0)
[Dapper] (https://github.com/DapperLib/Dapper)
[postgresql](https://www.postgresql.org/)

## Code Rules

To come.
Remember to format the code using `dotnet-format`

First install `dotnet-format` globally on your machine:

```sh
dotnet tool install -g dotnet-format
```

Then format the code using:

```sh
dotnet format
```

## PATCH REQUESTS

when doing patch requests. Have the body something like this:

```json
[
  {
    "op": "replace",
    "path": "/Name",
    "value": "Some new name"
  }
]
```

## Triggers

A couple of Postgres triggers are used in the system. Here are the complete up to date list of all the triggers.

## add_single_league_goal()

This function updates the single_league_matches table after on insert on the single_league_goals table

```sql
CREATE OR REPLACE FUNCTION add_single_league_goal()
RETURNS trigger AS
$$
DECLARE
  player_one_select integer := null;
  player_two_select integer := null;
  start_time_first timestamp := null;
BEGIN
	SELECT player_one into player_one_select FROM single_league_matches where id = new.match_id and player_one = new.scored_by_user_id;
	SELECT player_two into player_two_select FROM single_league_matches where id = new.match_id and player_two = new.scored_by_user_id;
	SELECT start_time into start_time_first FROM single_league_matches where id = new.match_id;

	if (player_one_select is not NULL) then
		update single_league_matches
		SET player_one_score = new.scorer_score
		where id = new.match_id;
	end if;

	if (player_two_select is not NULL) then
		update single_league_matches
		SET player_two_score = new.scorer_score
		where id = new.match_id;
	end if;

	if (new.winner_goal = true) then
		update single_league_matches
		SET match_ended = true
		where id = new.match_id;
	end if;

	if (start_time_first is null) then
		update single_league_matches
		set start_time = CURRENT_TIMESTAMP, match_started = true
		where id = new.match_id;
	end if;

	RETURN NEW;
END;
$$
LANGUAGE 'plpgsql';

CREATE TRIGGER add_single_league_goal
AFTER INSERT
ON single_league_goals
FOR EACH ROW
EXECUTE PROCEDURE add_single_league_goal();
```

## create_single_league_matches

This function runs after the single_league table is updated

```sql
CREATE OR REPLACE FUNCTION create_single_league_matches()
RETURNS trigger AS
$$
DECLARE
  user_ids integer[] := null;
  i integer;
  iterator integer := 0;
  array_length integer;
  counter integer := 0;
  counterMax integer := 2;
BEGIN
	if (NEW.has_league_started = true and OLD.has_league_started = false and old.type_of_league = 'single_league') then
		SELECT INTO user_ids array_agg(user_id) FROM league_players WHERE league_id = OLD.id;
		SELECT INTO array_length count(id) FROM league_players WHERE league_id = OLD.id;
		SELECT INTO counterMax how_many_rounds FROM leagues WHERE id = OLD.id;
		if (counterMax is NULL) then
			counterMax := 2;
		end if;
		LOOP
			exit when counter = counterMax;
			counter := counter + 1 ;
			iterator := 0;
			FOREACH i IN ARRAY user_ids
			LOOP
			iterator := iterator + 1;
			   FOR j in iterator .. array_length
			   LOOP
					IF (i != user_ids[j]) THEN
						INSERT INTO single_league_matches(player_one, player_two, league_id, start_time, end_time, player_one_score, player_two_score, match_ended, match_paused, match_started)
						VALUES(i, user_ids[j], OLD.id, null, null, 0, 0, false, false, false);
					END IF;
			   END LOOP;
			END LOOP;
		END LOOP;
	end if;
	RETURN NEW;
END;
$$
LANGUAGE 'plpgsql';

CREATE TRIGGER create_single_league_matches
AFTER UPDATE
ON leagues
FOR EACH ROW
EXECUTE PROCEDURE create_single_league_matches();
```

## end_single_league_match()

When the match is ended the database updates the current timestamp to the end_time field

```sql
CREATE OR REPLACE FUNCTION end_single_league_match()
RETURNS trigger AS
$$
BEGIN
	if (NEW.match_ended = true and OLD.match_ended = false) then
		update single_league_matches
		SET end_time = CURRENT_TIMESTAMP
		where id = old.id;
	end if;
	RETURN NEW;
END;
$$
LANGUAGE 'plpgsql';

CREATE TRIGGER end_single_league_match
AFTER UPDATE
ON single_league_matches
FOR EACH ROW
EXECUTE PROCEDURE end_single_league_match();
```

## start_single_league_match

When a match is started the database updates the current timestamp to the start_time field

```sql
CREATE OR REPLACE FUNCTION start_single_league_match()
RETURNS trigger AS
$$
BEGIN
	if (NEW.match_started = true and OLD.match_started = false and old.player_one_score = 0 and old.player_two_score = 0) then
		update single_league_matches
		SET start_time = CURRENT_TIMESTAMP
		where id = old.id;
	end if;
	RETURN NEW;
END;
$$
LANGUAGE 'plpgsql';

CREATE TRIGGER end_single_league_match
AFTER UPDATE
ON single_league_matches
FOR EACH ROW
EXECUTE PROCEDURE end_single_league_match();
```

## delete_single_league_goal

When a single league goal is deleted we want to update the score in the single_league_matches table
OBS!! This trigger was removed. Was making a mess. See if it is possible to have this trigger used
with AFTER DELETE instead of BEFORE DELETE

```sql
CREATE OR REPLACE FUNCTION delete_single_league_goal()
RETURNS trigger AS
$$
DECLARE
  user_who_scored integer := null;
  the_match_id integer := null;
  player_one_id integer := null;
  player_two_id integer := null;
BEGIN
    SELECT scored_by_user_id into user_who_scored FROM single_league_goals where id = OLD.id;
	SELECT match_id into the_match_id FROM single_league_goals where id = OLD.id;

	if (user_who_scored is not NULL) then
	    SELECT player_one into player_one_id FROM single_league_matches where id = the_match_id;
		SELECT player_two into player_two_id FROM single_league_matches where id = the_match_id;

		if (player_one_id = user_who_scored) then
			update single_league_matches
			SET player_one_score = player_one_score - 1
			where id = the_match_id;
		end if;

		if (player_two_id = user_who_scored) then
			update single_league_matches
			SET player_two_score = player_two_score - 1
			where id = the_match_id;
		end if;

	end if;
RETURN NEW;
END;
$$
LANGUAGE 'plpgsql';

CREATE TRIGGER delete_single_league_goal
BEFORE DELETE
ON single_league_goals
FOR EACH ROW
EXECUTE PROCEDURE delete_single_league_goal();
```

## create_double_league_matches

This function creates double_league_matches after update on the leagues table

```sql
CREATE OR REPLACE FUNCTION create_double_league_matches()
RETURNS trigger AS
$$
DECLARE
	team_ids integer[] := null;
	array_length integer;
    counterMax integer := 2;
	counter integer := 0;
	i integer;
    iterator integer := 0;
BEGIN
   if (NEW.has_league_started = true and OLD.has_league_started = false and old.type_of_league = 'double_league') then
   		SELECT INTO team_ids array_agg(dlt.id) FROM double_league_teams dlt WHERE dlt.league_id = OLD.id;
		SELECT INTO array_length count(dlt.id) FROM double_league_teams dlt WHERE dlt.league_id = OLD.id;
		SELECT INTO counterMax how_many_rounds FROM leagues WHERE id = OLD.id;
		if (counterMax is NULL) then
			counterMax := 2;
		end if;
			counterMax := counterMax * 2;
			LOOP
				exit when counter = counterMax;
				counter := counter + 1 ;
				iterator := 0;
				FOREACH i IN ARRAY team_ids
				LOOP
				iterator := iterator + 1;
				   FOR j in iterator .. array_length
				   LOOP
						IF (i != team_ids[j]) THEN
							INSERT INTO double_league_matches(team_one_id, team_two_id, league_id, start_time, end_time, team_one_score, team_two_score, match_started, match_ended, match_paused)
							VALUES(i, team_ids[j], OLD.id, null, null, 0, 0, false, false, false);
						END IF;
				   END LOOP;
				END LOOP;
			END LOOP;
   end if;
RETURN NEW;
END;
$$
LANGUAGE 'plpgsql';

CREATE TRIGGER create_double_league_matches
AFTER UPDATE
ON leagues
FOR EACH ROW
EXECUTE PROCEDURE create_double_league_matches();

```

## add_double_league_goal

This function updates the double_league_matches table after update on double_league_goals table

```sql
CREATE TRIGGER add_double_league_goal
AFTER INSERT
ON double_league_goals
FOR EACH ROW
EXECUTE PROCEDURE add_double_league_goal();




CREATE OR REPLACE FUNCTION add_double_league_goal()
RETURNS trigger AS
$$
DECLARE
  team_one_select integer := null;
  team_two_select integer := null;
  start_time_first timestamp := null;
BEGIN
	SELECT team_one_id into team_one_select FROM double_league_matches where id = new.match_id and team_one_id = new.scored_by_team_id;
	SELECT team_two_id into team_two_select FROM double_league_matches where id = new.match_id and team_two_id = new.scored_by_team_id;
	SELECT start_time into start_time_first FROM double_league_matches where id = new.match_id;

	if (team_one_select is not NULL) then
		update double_league_matches
		SET team_one_score = new.scorer_team_score, match_started = true
		where id = new.match_id;
	end if;

	if (team_two_select is not NULL) then
		update double_league_matches
		SET team_two_score = new.scorer_team_score, match_started = true
		where id = new.match_id;
	end if;

	if (new.winner_goal = true) then
		update double_league_matches
		SET match_ended = true, end_time = CURRENT_TIMESTAMP
		where id = new.match_id;
	end if;

	if (start_time_first is null) then
		update double_league_matches
		set start_time = CURRENT_TIMESTAMP
		where id = new.match_id;
	end if;

	RETURN NEW;
END;
$$
LANGUAGE 'plpgsql';

CREATE TRIGGER add_double_league_goal
AFTER INSERT
ON double_league_goals
FOR EACH ROW
EXECUTE PROCEDURE add_double_league_goal();

```

## Thanks

**Foosball** © 2021+, Mossfellsbær City. Released under the [MIT License].<br>
Authored and maintained by Daniel Freyr Sigurdsson. With help from [contributors].

<!-- > [ricostacruz.com](http://ricostacruz.com) &nbsp;&middot;&nbsp;
> GitHub [@rstacruz](https://github.com/rstacruz) &nbsp;&middot;&nbsp;
> Twitter [@rstacruz](https://twitter.com/rstacruz)

[mit license]: https://mit-license.org/
[contributors]: https://github.com/rstacruz/hicat/contributors
[highlight.js]: https://highlightjs.org -->

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

# To do list

These endpoints should be implemented

| Controller            | method | Name                | PATH                                   | Status |
| --------------------- | ------ | ------------------- | -------------------------------------- | ------ |
| Auth                  | POST   | Login               | Auth/login                             | x      |
| Auth                  | POST   | register            | Auth/register                          | x      |
| Auth                  | POST   | verify-email        | Auth/verify-email                      | x      |
| Auth                  | POST   | forgot-password     | Auth/forgot-password                   |        |
| Auth                  | POST   | reset-password      | Auth/reset-password                    |        |
| Cms                   | POST   | hardcoded-string    | Cms/hardcoded-strings                  | x      |
| DoubleLeagueGoals     | GET    | Get match by id     | DoubleLeagueGoals/match/{matchId}      | x      |
| DoubleLeagueGoals     | GET    | Get goal by goalId  | DoubleLeagueGoals/{goalId}             | x      |
| DoubleLeagueGoals     | DELETE | Delete dlg by ID    | DoubleLeagueGoals/{goalId}             | x      |
| DoubleLeagueGoals     | POST   | Create new goal     | DoubleLeagueGoals                      | x      |
| DoubleLeagueMatches   | GET    | Get all Dl matches  | DoubleLeagueMatches                    | x      |
| DoubleLeagueMatches   | PATCH  | Update dl match     | DoubleLeagueMatches                    | x      |
| DoubleLeagueMatches   | GET    | Get match by id     | DoubleLeagueMatches/match/{matchId}    | x      |
| DoubleLeagueMatches   | PUT    | reset-match         | DoubleLeagueMatches/reset-match        | x      |
| DoubleLeaguePlayers   | GET    | Gel all league pl   | DoubleLeaguePlayers/{leagueId}         | x      |
| DoubleLeaguePlayers   | GET    | Get player by id    | DoubleLeaguePlayers/player/{id}        | x      |
| DoubleLeagueTeams     | GET    | Get teams by l. id  | DoubleLeagueTeams/{leagueId}           | x      |
| DoubleLeagueTeams     | GET    | Get team by id      | DoubleLeagueTeams/team/{id}            | x      |
| DoubleLeagueTeams     | POST   | Create new league   | DoubleLeagueTeams/{leagueId}/{teamId}  | x      |
| DoubleLeagueTeams     | DELETE | Delete league       | DoubleLeagueTeams/{leagueId}/{teamId}  | x      |
| FreehandDoubleGoals   | GET    | Get dlga by matchId | FreehandDoubleGoals/goals/{matchId}    | x      |
| FreehandDoubleGoals   | GET    | get dlb by id       | FreehandDoubleGoals/{goalId}           | x      |
| FreehandDoubleGoals   | POST   | Create dlg goal     | FreehandDoubleGoals                    | x      |
| FreehandDoubleGoals   | PATCH  | UPDATE dlg goal     | FreehandDoubleGoals                    | x      |
| FreehandDoubleGoals   | DELETE | Delete dlg goal     | FreehandDoubleGoals/{matchId}/{goalId} | x      |
| FreehandDoubleMatches | GET    | Get fh matches      | FreehandDoubleMatches                  | x      |
| FreehandDoubleMatches | POST   | Create fh match     | FreehandDoubleMatches                  | x      |
| FreehandDoubleMatches | PATCH  | Update fdm          | FreehandDoubleMatches                  | x      |
| FreehandDoubleMatches | GET    | Get fdm by matchId  | FreehandDoubleMatches/{matchId}        | x      |
| FreehandDoubleMatches | DELETE | Delete fdm          | FreehandDoubleMatches/{matchId}        | x      |
| FreehandGoals         | GET    | Get goals by mId    | FreehandGoals/goals/{matchId}          | x      |
| FreehandGoals         | GET    | Get goal by gId     | FreehandGoals/{goalId}                 | x      |
| FreehandGoals         | POST   | Create f goal       | FreehandGoals                          | x      |
| FreehandGoals         | PATCH  | Update f goal       | FreehandGoals                          | x      |
| FreehandGoals         | DELETE | Delete f goal       | FreehandGoals/{matchId}/{goalId}       | x      |
| FreehandMatches       | GET    | Get f matches       | FreehandMatches                        | x      |
| FreehandMatches       | POST   | Create f match      | FreehandMatches                        | x      |
| FreehandMatches       | PATCH  | Update f match      | FreehandMatches                        | x      |
| FreehandMatches       | GET    | Get f match by mID  | FreehandMatches/{matchId}              | x      |
| FreehandMatches       | DELETE | Delete f match      | FreehandMatches/{matchId}              | x      |
| Leagues               | GET    | Get leagus by org.  | Leagues/organisation                   |        |
| Leagues               | GET    | Get league by id    | Leagues/{id}                           |        |
| Leagues               | PATCH  | UPDATE league by id | Leagues/{id}                           |        |
| Leagues               | GET    | Get league players  | Leagues/league-players                 |        |
| Leagues               | POST   | Create new league   | Leagues                                |        |
| Leagues               | DELETE | Delete league by id | Leagues/{leagueId}                     |        |
| Leagues               | GET    | Get leag. standings | Leagues/single-league/standings        |        |
| Leagues               | GET    | Get dLeague stand.  | Leagues/double-league/standings        |        |
| Organisations         | GET    | Get organisations   | Organisations                          |        |
| Organisations         | POST   | Create organisation | Organisations                          |        |
| Organisations         | GET    | Get org by id       | Organisations/{id}                     |        |
| Organisations         | PATCH  | Update org by id    | Organisations/{id}                     |        |
| Organisations         | DELETE | Delete org by id    | Organisations/{id}                     |        |
| Organisations         | GET    | Get orgs by user    | Organisations/user                     |        |
| SingleLeagueGoals     | GET    | Get sl goals        | SingleLeagueGoals                      |        |
| SingleLeagueGoals     | POST   | Create sl goal      | SingleLeagueGoals                      |        |
| SingleLeagueGoals     | GET    | Get sl goal by gId  | SingleLeagueGoals/{goalId}             |        |
| SingleLeagueGoals     | DELETE | Delete slgoal by id | SingleLeagueGoals/{goalId}             |        |
| SingleLeagueMatches   | GET    | Get sl matches      | SingleLeagueMatches                    |        |
| SingleLeagueMatches   | PATCH  | Update sl match     | SingleLeagueMatches                    |        |
| SingleLeagueMatches   | GET    | Get sl match        | SingleLeagueMatches/{matchId}          |        |
| SingleLeagueMatches   | PUT    | Reset sl match      | SingleLeagueMatches/reset-match        |        |
| Users                 | GET    | Get users           | Users                                  | x      |
| Users                 | GET    | Get user by id      | Users/{id}                             | x      |
| Users                 | PATCH  | Update user by id   | Users/{id}                             | x      |
| Users                 | DELETE | Delete user by id   | Users/{id}                             | x      |
| Users                 | GET    | Get user stats      | Users/stats                            | x      |
| Users                 | GET    | Get last 10 matches | Users/last-ten-matches                 | x      |
| Users                 | GET    | Get history         | Users/history                          | x      |
