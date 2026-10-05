using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalcioAnalytic.Infrastructure.Persistence.Migrations;

public partial class AddMatchFeatureSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS analytics.match_feature_snapshots (
                "Id" uuid NOT NULL,
                "MatchId" uuid NOT NULL,
                "KickoffUtc" timestamp with time zone NOT NULL,
                "FeatureTimestampUtc" timestamp with time zone NOT NULL,
                "HomeElo" numeric NULL,
                "AwayElo" numeric NULL,
                "EloDifference" numeric NULL,
                "HomeFormLast5Points" integer NULL,
                "AwayFormLast5Points" integer NULL,
                "HomeGoalsForLast5" numeric NULL,
                "HomeGoalsAgainstLast5" numeric NULL,
                "AwayGoalsForLast5" numeric NULL,
                "AwayGoalsAgainstLast5" numeric NULL,
                "PoissonHomeLambda" numeric NULL,
                "PoissonAwayLambda" numeric NULL,
                "PoissonHomeProbability" numeric NULL,
                "PoissonDrawProbability" numeric NULL,
                "PoissonAwayProbability" numeric NULL,
                "DixonColesHomeProbability" numeric NULL,
                "DixonColesDrawProbability" numeric NULL,
                "DixonColesAwayProbability" numeric NULL,
                "MarketHomeProbability" numeric NULL,
                "MarketDrawProbability" numeric NULL,
                "MarketAwayProbability" numeric NULL,
                "HomeClosingOdds" numeric NULL,
                "DrawClosingOdds" numeric NULL,
                "AwayClosingOdds" numeric NULL,
                "FeatureSetVersion" character varying(50) NOT NULL,
                "MethodologyVersion" character varying(50) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL,
                CONSTRAINT "PK_match_feature_snapshots" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_match_feature_snapshots_matches_MatchId"
                    FOREIGN KEY ("MatchId") REFERENCES matches.matches ("Id") ON DELETE RESTRICT,
                CONSTRAINT "ck_match_feature_snapshot_pre_kickoff"
                    CHECK ("FeatureTimestampUtc" < "KickoffUtc")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_match_feature_snapshots_MatchId_FeatureTimestampUtc"
                ON analytics.match_feature_snapshots ("MatchId", "FeatureTimestampUtc");

            CREATE INDEX IF NOT EXISTS "IX_match_feature_snapshots_KickoffUtc_FeatureTimestampUtc"
                ON analytics.match_feature_snapshots ("KickoffUtc", "FeatureTimestampUtc");

            CREATE INDEX IF NOT EXISTS "IX_match_feature_snapshots_FeatureSetVersion"
                ON analytics.match_feature_snapshots ("FeatureSetVersion");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS analytics.match_feature_snapshots;
            """);
    }
}
