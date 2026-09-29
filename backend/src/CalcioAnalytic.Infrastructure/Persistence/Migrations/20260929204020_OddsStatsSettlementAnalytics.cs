using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalcioAnalytic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OddsStatsSettlementAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "odds");

            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.EnsureSchema(
                name: "statistics");

            migrationBuilder.CreateTable(
                name: "market_lines",
                schema: "odds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    Line = table.Column<decimal>(type: "numeric", nullable: true),
                    Period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_market_lines_markets_MarketId",
                        column: x => x.MarketId,
                        principalSchema: "catalog",
                        principalTable: "markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_market_lines_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "match_analyses",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnalysisJson = table.Column<string>(type: "text", nullable: false),
                    MethodologyVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_analyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_analyses_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "match_events",
                schema: "statistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Minute = table.Column<int>(type: "integer", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_events_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_match_events_players_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "catalog",
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_match_events_teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "catalog",
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "match_statistics",
                schema: "statistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_statistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_statistics_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_match_statistics_teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "catalog",
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "selections",
                schema: "odds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_selections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_selections_market_lines_MarketLineId",
                        column: x => x.MarketLineId,
                        principalSchema: "odds",
                        principalTable: "market_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "market_settlements",
                schema: "odds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_market_settlements_market_lines_MarketLineId",
                        column: x => x.MarketLineId,
                        principalSchema: "odds",
                        principalTable: "market_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_market_settlements_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_market_settlements_selections_SelectionId",
                        column: x => x.SelectionId,
                        principalSchema: "odds",
                        principalTable: "selections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "odds_snapshots",
                schema: "odds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookmakerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecimalOdds = table.Column<decimal>(type: "numeric", nullable: false),
                    FractionalOdds = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AmericanOdds = table.Column<int>(type: "integer", nullable: true),
                    ImpliedProbability = table.Column<decimal>(type: "numeric", nullable: false),
                    IsLive = table.Column<bool>(type: "boolean", nullable: false),
                    MatchMinute = table.Column<int>(type: "integer", nullable: true),
                    Period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BookmakerTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProviderTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IngestionTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_odds_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_odds_snapshots_bookmakers_BookmakerId",
                        column: x => x.BookmakerId,
                        principalSchema: "catalog",
                        principalTable: "bookmakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odds_snapshots_market_lines_MarketLineId",
                        column: x => x.MarketLineId,
                        principalSchema: "odds",
                        principalTable: "market_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odds_snapshots_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odds_snapshots_selections_SelectionId",
                        column: x => x.SelectionId,
                        principalSchema: "odds",
                        principalTable: "selections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_market_lines_MarketId",
                schema: "odds",
                table: "market_lines",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_market_lines_MatchId_MarketId",
                schema: "odds",
                table: "market_lines",
                columns: new[] { "MatchId", "MarketId" });

            migrationBuilder.CreateIndex(
                name: "IX_market_settlements_MarketLineId",
                schema: "odds",
                table: "market_settlements",
                column: "MarketLineId");

            migrationBuilder.CreateIndex(
                name: "IX_market_settlements_MatchId_MarketLineId_SelectionId",
                schema: "odds",
                table: "market_settlements",
                columns: new[] { "MatchId", "MarketLineId", "SelectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_market_settlements_SelectionId",
                schema: "odds",
                table: "market_settlements",
                column: "SelectionId");

            migrationBuilder.CreateIndex(
                name: "IX_match_analyses_MatchId_Version",
                schema: "analytics",
                table: "match_analyses",
                columns: new[] { "MatchId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_events_MatchId",
                schema: "statistics",
                table: "match_events",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_match_events_PlayerId",
                schema: "statistics",
                table: "match_events",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_match_events_TeamId",
                schema: "statistics",
                table: "match_events",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_match_statistics_MatchId_TeamId",
                schema: "statistics",
                table: "match_statistics",
                columns: new[] { "MatchId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_match_statistics_TeamId",
                schema: "statistics",
                table: "match_statistics",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_BookmakerId",
                schema: "odds",
                table: "odds_snapshots",
                column: "BookmakerId");

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_MarketLineId",
                schema: "odds",
                table: "odds_snapshots",
                column: "MarketLineId");

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_MatchId_BookmakerId_MarketLineId_SelectionId~",
                schema: "odds",
                table: "odds_snapshots",
                columns: new[] { "MatchId", "BookmakerId", "MarketLineId", "SelectionId", "ProviderTimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_PayloadHash",
                schema: "odds",
                table: "odds_snapshots",
                column: "PayloadHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_odds_snapshots_SelectionId",
                schema: "odds",
                table: "odds_snapshots",
                column: "SelectionId");

            migrationBuilder.CreateIndex(
                name: "IX_selections_MarketLineId",
                schema: "odds",
                table: "selections",
                column: "MarketLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_settlements",
                schema: "odds");

            migrationBuilder.DropTable(
                name: "match_analyses",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "match_events",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "match_statistics",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "odds_snapshots",
                schema: "odds");

            migrationBuilder.DropTable(
                name: "selections",
                schema: "odds");

            migrationBuilder.DropTable(
                name: "market_lines",
                schema: "odds");
        }
    }
}
