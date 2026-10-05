using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalcioAnalytic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOddsSnapshotProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_odds_snapshots_MatchId_BookmakerId_MarketLineId_SelectionId~",
                schema: "odds",
                table: "odds_snapshots");

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                schema: "odds",
                table: "odds_snapshots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "match_feature_snapshots",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    KickoffUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FeatureTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HomeElo = table.Column<decimal>(type: "numeric", nullable: true),
                    AwayElo = table.Column<decimal>(type: "numeric", nullable: true),
                    EloDifference = table.Column<decimal>(type: "numeric", nullable: true),
                    HomeFormLast5Points = table.Column<int>(type: "integer", nullable: true),
                    AwayFormLast5Points = table.Column<int>(type: "integer", nullable: true),
                    HomeGoalsForLast5 = table.Column<decimal>(type: "numeric", nullable: true),
                    HomeGoalsAgainstLast5 = table.Column<decimal>(type: "numeric", nullable: true),
                    AwayGoalsForLast5 = table.Column<decimal>(type: "numeric", nullable: true),
                    AwayGoalsAgainstLast5 = table.Column<decimal>(type: "numeric", nullable: true),
                    PoissonHomeLambda = table.Column<decimal>(type: "numeric", nullable: true),
                    PoissonAwayLambda = table.Column<decimal>(type: "numeric", nullable: true),
                    PoissonHomeProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    PoissonDrawProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    PoissonAwayProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    DixonColesHomeProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    DixonColesDrawProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    DixonColesAwayProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    MarketHomeProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    MarketDrawProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    MarketAwayProbability = table.Column<decimal>(type: "numeric", nullable: true),
                    HomeClosingOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    DrawClosingOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    AwayClosingOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    FeatureSetVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MethodologyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_feature_snapshots", x => x.Id);
                    table.CheckConstraint("ck_match_feature_snapshot_pre_kickoff", "\"FeatureTimestampUtc\" < \"KickoffUtc\"");
                    table.ForeignKey(
                        name: "FK_match_feature_snapshots_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_MatchId_ProviderId_BookmakerId_MarketLineId_~",
                schema: "odds",
                table: "odds_snapshots",
                columns: new[] { "MatchId", "ProviderId", "BookmakerId", "MarketLineId", "SelectionId", "ProviderTimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_ProviderId",
                schema: "odds",
                table: "odds_snapshots",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_match_feature_snapshots_FeatureSetVersion",
                schema: "analytics",
                table: "match_feature_snapshots",
                column: "FeatureSetVersion");

            migrationBuilder.CreateIndex(
                name: "IX_match_feature_snapshots_KickoffUtc_FeatureTimestampUtc",
                schema: "analytics",
                table: "match_feature_snapshots",
                columns: new[] { "KickoffUtc", "FeatureTimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_match_feature_snapshots_MatchId_FeatureTimestampUtc",
                schema: "analytics",
                table: "match_feature_snapshots",
                columns: new[] { "MatchId", "FeatureTimestampUtc" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_odds_snapshots_providers_ProviderId",
                schema: "odds",
                table: "odds_snapshots",
                column: "ProviderId",
                principalSchema: "catalog",
                principalTable: "providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_odds_snapshots_providers_ProviderId",
                schema: "odds",
                table: "odds_snapshots");

            migrationBuilder.DropTable(
                name: "match_feature_snapshots",
                schema: "analytics");

            migrationBuilder.DropIndex(
                name: "IX_odds_snapshots_MatchId_ProviderId_BookmakerId_MarketLineId_~",
                schema: "odds",
                table: "odds_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_odds_snapshots_ProviderId",
                schema: "odds",
                table: "odds_snapshots");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "odds",
                table: "odds_snapshots");

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_MatchId_BookmakerId_MarketLineId_SelectionId~",
                schema: "odds",
                table: "odds_snapshots",
                columns: new[] { "MatchId", "BookmakerId", "MarketLineId", "SelectionId", "ProviderTimestampUtc" });
        }
    }
}
