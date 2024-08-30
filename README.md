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

## notify freehand match change

When a freehand match is changed

```sql
CREATE OR REPLACE FUNCTION notify_score_update() RETURNS trigger AS $$
BEGIN
    IF NEW.game_finished = false THEN
        PERFORM pg_notify(
            'score_update', 
            json_build_object(
                'match_id', NEW.id,
                'player_one_id', NEW.player_one_id,
                'player_two_id', NEW.player_two_id,
                'player_one_score', NEW.player_one_score,
                'player_two_score', NEW.player_two_score,
                'start_time', NEW.start_time,
                'end_time', NEW.end_time,
                'up_to', NEW.up_to,
                'game_finished', NEW.game_finished,
                'game_paused', NEW.game_paused,
                'organisation_id', NEW.organisation_id,
                'player_one', (
                    SELECT json_build_object(
                        'id', u1.id,
                        'first_name', COALESCE(u1.first_name, 'Unknown'),
                        'last_name', COALESCE(u1.last_name, 'Unknown'),
                        'photo_url', COALESCE(u1.photo_url, 'default_image_url')
                    )
                    FROM users u1
                    WHERE u1.id = NEW.player_one_id
                ),
                'player_two', (
                    SELECT json_build_object(
                        'id', u2.id,
                        'first_name', COALESCE(u2.first_name, 'Unknown'),
                        'last_name', COALESCE(u2.last_name, 'Unknown'),
                        'photo_url', COALESCE(u2.photo_url, 'default_image_url')
                    )
                    FROM users u2
                    WHERE u2.id = NEW.player_two_id
                )
            )::text
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER score_update_trigger
AFTER UPDATE OF player_one_score, player_two_score ON freehand_matches
FOR EACH ROW
WHEN (OLD.player_one_score IS DISTINCT FROM NEW.player_one_score OR
      OLD.player_two_score IS DISTINCT FROM NEW.player_two_score)
EXECUTE FUNCTION notify_score_update();

CREATE TRIGGER score_insert_trigger
AFTER INSERT ON freehand_matches
FOR EACH ROW
EXECUTE FUNCTION notify_score_insert();

CREATE OR REPLACE FUNCTION notify_score_insert() RETURNS trigger AS $$
BEGIN
    IF NEW.game_finished = false THEN
        PERFORM pg_notify(
            'score_update', 
            json_build_object(
                'match_id', NEW.id,
                'player_one_id', NEW.player_one_id,
                'player_two_id', NEW.player_two_id,
                'player_one_score', NEW.player_one_score,
                'player_two_score', NEW.player_two_score,
                'start_time', NEW.start_time,
                'end_time', NEW.end_time,
                'up_to', NEW.up_to,
                'game_finished', NEW.game_finished,
                'game_paused', NEW.game_paused,
                'organisation_id', NEW.organisation_id,
                'player_one', (
                    SELECT json_build_object(
                        'id', u1.id,
                        'first_name', COALESCE(u1.first_name, 'Unknown'),
                        'last_name', COALESCE(u1.last_name, 'Unknown'),
                        'photo_url', COALESCE(u1.photo_url, 'default_image_url')
                    )
                    FROM users u1
                    WHERE u1.id = NEW.player_one_id
                ),
                'player_two', (
                    SELECT json_build_object(
                        'id', u2.id,
                        'first_name', COALESCE(u2.first_name, 'Unknown'),
                        'last_name', COALESCE(u2.last_name, 'Unknown'),
                        'photo_url', COALESCE(u2.photo_url, 'default_image_url')
                    )
                    FROM users u2
                    WHERE u2.id = NEW.player_two_id
                )
            )::text
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION notify_double_score_update() RETURNS trigger AS $$
BEGIN
    IF NEW.game_finished = false THEN
        PERFORM pg_notify(
            'double_score_update', 
            json_build_object(
                'match_id', NEW.id,
                'team_a_player_one_id', NEW.player_one_team_a,
                'team_a_player_two_id', NEW.player_two_team_a,
                'team_b_player_one_id', NEW.player_one_team_b,
                'team_b_player_two_id', NEW.player_two_team_b,
                'team_a_score', NEW.team_a_score,
                'team_b_score', NEW.team_b_score,
                'start_time', NEW.start_time,
                'end_time', NEW.end_time,
                'up_to', NEW.up_to,
                'game_finished', NEW.game_finished,
                'game_paused', NEW.game_paused,
                'organisation_id', NEW.organisation_id,
                'team_a_player_one', (
                    SELECT json_build_object(
                        'id', u1.id,
                        'first_name', COALESCE(u1.first_name, 'Unknown'),
                        'last_name', COALESCE(u1.last_name, 'Unknown'),
                        'photo_url', COALESCE(u1.photo_url, 'default_image_url')
                    )
                    FROM users u1
                    WHERE u1.id = NEW.player_one_team_a
                ),
                'team_a_player_two', (
                    SELECT json_build_object(
                        'id', u2.id,
                        'first_name', COALESCE(u2.first_name, 'Unknown'),
                        'last_name', COALESCE(u2.last_name, 'Unknown'),
                        'photo_url', COALESCE(u2.photo_url, 'default_image_url')
                    )
                    FROM users u2
                    WHERE u2.id = NEW.player_two_team_a
                ),
                'team_b_player_one', (
                    SELECT json_build_object(
                        'id', u3.id,
                        'first_name', COALESCE(u3.first_name, 'Unknown'),
                        'last_name', COALESCE(u3.last_name, 'Unknown'),
                        'photo_url', COALESCE(u3.photo_url, 'default_image_url')
                    )
                    FROM users u3
                    WHERE u3.id = NEW.player_one_team_b
                ),
                'team_b_player_two', (
                    SELECT json_build_object(
                        'id', u4.id,
                        'first_name', COALESCE(u4.first_name, 'Unknown'),
                        'last_name', COALESCE(u4.last_name, 'Unknown'),
                        'photo_url', COALESCE(u4.photo_url, 'default_image_url')
                    )
                    FROM users u4
                    WHERE u4.id = NEW.player_two_team_b
                ),
                'last_goal', (
                    SELECT json_build_object(
                        'scored_by_user_id', g.scored_by_user_id,
                        'scorer_team_score', g.scorer_team_score,
                        'opponent_team_score', g.opponent_team_score,
                        'time_of_goal', g.time_of_goal,
                        'winner_goal', g.winner_goal,
                        'scorer', (
                            SELECT json_build_object(
                                'id', u5.id,
                                'first_name', COALESCE(u5.first_name, 'Unknown'),
                                'last_name', COALESCE(u5.last_name, 'Unknown'),
                                'photo_url', COALESCE(u5.photo_url, 'default_image_url')
                            )
                            FROM users u5
                            WHERE u5.id = g.scored_by_user_id
                        )
                    )
                    FROM freehand_double_goals g
                    WHERE g.double_match_id = NEW.id
                    ORDER BY g.time_of_goal DESC
                    LIMIT 1
                )
            )::text
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION notify_double_score_insert() RETURNS trigger AS $$
BEGIN
	RAISE NOTICE 'Trigger Fired on Insert: %', NEW.id;
    IF NEW.game_finished = false THEN
		RAISE NOTICE 'IF STATEMENT is here %', NEW.id;
        PERFORM pg_notify(
            'double_score_update', 
            json_build_object(
                'match_id', NEW.id,
                'team_a_player_one_id', NEW.player_one_team_a,
                'team_a_player_two_id', NEW.player_two_team_a,
                'team_b_player_one_id', NEW.player_one_team_b,
                'team_b_player_two_id', NEW.player_two_team_b,
                'team_a_score', NEW.team_a_score,
                'team_b_score', NEW.team_b_score,
                'start_time', NEW.start_time,
                'end_time', NEW.end_time,
                'up_to', NEW.up_to,
                'game_finished', NEW.game_finished,
                'game_paused', NEW.game_paused,
                'organisation_id', NEW.organisation_id,
                'team_a_player_one', (
                    SELECT json_build_object(
                        'id', u1.id,
                        'first_name', COALESCE(u1.first_name, 'Unknown'),
                        'last_name', COALESCE(u1.last_name, 'Unknown'),
                        'photo_url', COALESCE(u1.photo_url, 'default_image_url')
                    )
                    FROM users u1
                    WHERE u1.id = NEW.player_one_team_a
                ),
                'team_a_player_two', (
                    SELECT json_build_object(
                        'id', u2.id,
                        'first_name', COALESCE(u2.first_name, 'Unknown'),
                        'last_name', COALESCE(u2.last_name, 'Unknown'),
                        'photo_url', COALESCE(u2.photo_url, 'default_image_url')
                    )
                    FROM users u2
                    WHERE u2.id = NEW.player_two_team_a
                ),
                'team_b_player_one', (
                    SELECT json_build_object(
                        'id', u3.id,
                        'first_name', COALESCE(u3.first_name, 'Unknown'),
                        'last_name', COALESCE(u3.last_name, 'Unknown'),
                        'photo_url', COALESCE(u3.photo_url, 'default_image_url')
                    )
                    FROM users u3
                    WHERE u3.id = NEW.player_one_team_b
                ),
                'team_b_player_two', (
                    SELECT json_build_object(
                        'id', u4.id,
                        'first_name', COALESCE(u4.first_name, 'Unknown'),
                        'last_name', COALESCE(u4.last_name, 'Unknown'),
                        'photo_url', COALESCE(u4.photo_url, 'default_image_url')
                    )
                    FROM users u4
                    WHERE u4.id = NEW.player_two_team_b
                ),
                'last_goal', (
                    SELECT json_build_object(
                        'scored_by_user_id', g.scored_by_user_id,
                        'scorer_team_score', g.scorer_team_score,
                        'opponent_team_score', g.opponent_team_score,
                        'time_of_goal', g.time_of_goal,
                        'winner_goal', g.winner_goal,
                        'scorer', (
                            SELECT json_build_object(
                                'id', u5.id,
                                'first_name', COALESCE(u5.first_name, 'Unknown'),
                                'last_name', COALESCE(u5.last_name, 'Unknown'),
                                'photo_url', COALESCE(u5.photo_url, 'default_image_url')
                            )
                            FROM users u5
                            WHERE u5.id = g.scored_by_user_id
                        )
                    )
                    FROM freehand_double_goals g
                    WHERE g.double_match_id = NEW.id
                    ORDER BY g.time_of_goal DESC
                    LIMIT 1
                )
            )::text
        );
	RAISE NOTICE 'end is here: Player One Team A ID: %, Score: %', NEW.player_one_team_a, NEW.team_a_score;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


CREATE TRIGGER double_score_update_trigger
AFTER UPDATE OF team_a_score, team_b_score ON freehand_double_matches
FOR EACH ROW
WHEN (OLD.team_a_score IS DISTINCT FROM NEW.team_a_score OR
      OLD.team_b_score IS DISTINCT FROM NEW.team_b_score)
EXECUTE FUNCTION notify_double_score_update();

CREATE TRIGGER double_score_insert_trigger
AFTER INSERT ON freehand_double_matches
FOR EACH ROW
EXECUTE FUNCTION notify_double_score_insert();

CREATE OR REPLACE FUNCTION notify_single_league_score_update() RETURNS trigger AS $$
DECLARE
    last_goal json;
BEGIN
    -- Check if the match is ongoing and not ended
    IF NEW.match_ended = false AND NEW.match_started = true THEN
        -- Get the most recent goal information for this match (if any)
        SELECT json_build_object(
            'scored_by_user_id', g.scored_by_user_id,
            'scorer_team_score', g.scorer_score,
            'opponent_team_score', g.opponent_score,
            'time_of_goal', g.time_of_goal,
            'winner_goal', g.winner_goal,
            'scorer', (
                SELECT json_build_object(
                    'id', u1.id,
                    'first_name', COALESCE(u1.first_name, 'Unknown'),
                    'last_name', COALESCE(u1.last_name, 'Unknown'),
                    'photo_url', COALESCE(u1.photo_url, 'default_image_url')
                )
                FROM users u1
                WHERE u1.id = g.scored_by_user_id
            )
        ) INTO last_goal
        FROM single_league_goals g
        WHERE g.match_id = NEW.id
        ORDER BY g.time_of_goal DESC
        LIMIT 1;

        -- Notify the listeners with match and goal information
        PERFORM pg_notify(
            'single_league_score_update', 
            json_build_object(
                'match_id', NEW.id,
                'player_one_id', NEW.player_one,
                'player_two_id', NEW.player_two,
                'player_one_score', NEW.player_one_score,
                'player_two_score', NEW.player_two_score,
                'start_time', NEW.start_time,
                'end_time', NEW.end_time,
                'up_to', NEW.up_to,
                'match_ended', NEW.match_ended,
                'match_paused', NEW.match_paused,
                'organisation_id', NEW.organisation_id,
                'LastGoal', last_goal,
                'player_one', (
                    SELECT json_build_object(
                        'id', u1.id,
                        'first_name', COALESCE(u1.first_name, 'Unknown'),
                        'last_name', COALESCE(u1.last_name, 'Unknown'),
                        'photo_url', COALESCE(u1.photo_url, 'default_image_url')
                    )
                    FROM users u1
                    WHERE u1.id = NEW.player_one
                ),
                'player_two', (
                    SELECT json_build_object(
                        'id', u2.id,
                        'first_name', COALESCE(u2.first_name, 'Unknown'),
                        'last_name', COALESCE(u2.last_name, 'Unknown'),
                        'photo_url', COALESCE(u2.photo_url, 'default_image_url')
                    )
                    FROM users u2
                    WHERE u2.id = NEW.player_two
                )
            )::text
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER notify_single_league_score_update_trigger
AFTER UPDATE ON single_league_matches
FOR EACH ROW
WHEN (NEW.match_started = true AND NEW.match_ended = false)
EXECUTE FUNCTION notify_single_league_score_update();

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
| Leagues               | GET    | Get leagus by org.  | Leagues/organisation                   | x      |
| Leagues               | GET    | Get league by id    | Leagues/{id}                           | x      |
| Leagues               | PATCH  | UPDATE league by id | Leagues/{id}                           | x      |
| Leagues               | GET    | Get league players  | Leagues/league-players                 | x      |
| Leagues               | POST   | Create new league   | Leagues                                | x      |
| Leagues               | DELETE | Delete league by id | Leagues/{leagueId}                     | x      |
| Leagues               | GET    | Get leag. standings | Leagues/single-league/standings        | x      |
| Leagues               | GET    | Get dLeague stand.  | Leagues/double-league/standings        | x      |
| Organisations         | POST   | Create organisation | Organisations                          | x      |
| Organisations         | GET    | Get org by id       | Organisations/{id}                     | x      |
| Organisations         | PATCH  | Update org by id    | Organisations/{id}                     | x      |
| Organisations         | DELETE | Delete org by id    | Organisations/{id}                     | x      |
| Organisations         | GET    | Get orgs by user    | Organisations/user                     | x      |
| SingleLeagueGoals     | GET    | Get sl goals        | SingleLeagueGoals                      | x      |
| SingleLeagueGoals     | POST   | Create sl goal      | SingleLeagueGoals                      | x      |
| SingleLeagueGoals     | GET    | Get sl goal by gId  | SingleLeagueGoals/{goalId}             | x      |
| SingleLeagueGoals     | DELETE | Delete slgoal by id | SingleLeagueGoals/{goalId}             | x      |
| SingleLeagueMatches   | GET    | Get sl matches      | SingleLeagueMatches                    | x      |
| SingleLeagueMatches   | PATCH  | Update sl match     | SingleLeagueMatches                    | x      |
| SingleLeagueMatches   | GET    | Get sl match        | SingleLeagueMatches/{matchId}          | x      |
| SingleLeagueMatches   | PUT    | Reset sl match      | SingleLeagueMatches/reset-match        | x      |
| Users                 | GET    | Get users           | Users                                  | x      |
| Users                 | GET    | Get user by id      | Users/{id}                             | x      |
| Users                 | PATCH  | Update user by id   | Users/{id}                             | x      |
| Users                 | DELETE | Delete user by id   | Users/{id}                             | x      |
| Users                 | GET    | Get user stats      | Users/stats                            | x      |
| Users                 | GET    | Get last 10 matches | Users/last-ten-matches                 | x      |
| Users                 | GET    | Get history         | Users/history                          | x      |

# Https dev-certs

dotnet dev-certs https --clean
dotnet dev-certs https --trust

# Tmp, need to add this to live database

ALTER TABLE organisations
ADD organisation_code TEXT

ALTER TABLE organisations
ADD CONSTRAINT orgnisation_code_unique UNIQUE (organisation_code);

CREATE UNIQUE INDEX idx_organisation_code
ON organisations(organisation_code);

ALTER TABLE organisation_list
ADD is_admin boolean

alter table organisation_list
add column "is_deleted" BOOLEAN DEFAULT FALSE

ALTER TABLE users
ADD refresh_token varchar,
ADD refresh_token_expiry_time timestamp;

ALTER TABLE users
ADD refresh_token_web varchar,
ADD refresh_token_web_expiry_time timestamp;

CREATE TABLE old_refresh_tokens (
id SERIAL PRIMARY KEY,
refresh_token CHARACTER VARYING NOT NULL,
refresh_token_expiry_time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
fk_user_id INTEGER REFERENCES users(id),
fk_organisation_id INTEGER REFERENCES organisations(id)
);

ALTER TABLE old_refresh_tokens
ADD inserted_at timestamp

ALTER TABLE verifications
ADD change_password_token text;

ALTER TABLE verifications
ADD change_password_token_expires timestamp without time zone;

ALTER TABLE verifications
ADD COLUMN change_password_verification_token text

ALTER TABLE organisations
ADD COLUMN slack_webhook_url text

ollama pull phi3:mini

ALTER TABLE organisations
ADD COLUMN discord_webhook_url TEXT;

ALTER TABLE organisations
ADD COLUMN microsoft_teams_webhook_url TEXT;