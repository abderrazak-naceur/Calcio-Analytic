using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalcioAnalytic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOddsLifecycleSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "odds_lifecycle_summaries",
                schema: "odds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookmakerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpeningOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    OpeningTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreKickoffOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    PreKickoffTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosingOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    ClosingTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MinOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxOdds = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_odds_lifecycle_summaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_odds_lifecycle_summaries_bookmakers_BookmakerId",
                        column: x => x.BookmakerId,
                        principalSchema: "catalog",
                        principalTable: "bookmakers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odds_lifecycle_summaries_market_lines_MarketLineId",
                        column: x => x.MarketLineId,
                        principalSchema: "odds",
                        principalTable: "market_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odds_lifecycle_summaries_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "matches",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odds_lifecycle_summaries_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "catalog",
                        principalTable: "providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odds_lifecycle_summaries_selections_SelectionId",
                        column: x => x.SelectionId,
                        principalSchema: "odds",
                        principalTable: "selections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_odds_lifecycle_summaries_BookmakerId",
                schema: "odds",
                table: "odds_lifecycle_summaries",
                column: "BookmakerId");

            migrationBuilder.CreateIndex(
                name: "IX_odds_lifecycle_summaries_MarketLineId",
                schema: "odds",
                table: "odds_lifecycle_summaries",
                column: "MarketLineId");

            migrationBuilder.CreateIndex(
                name: "IX_odds_lifecycle_summaries_MatchId_BookmakerId_MarketLineId",
                schema: "odds",
                table: "odds_lifecycle_summaries",
                columns: new[] { "MatchId", "BookmakerId", "MarketLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_odds_lifecycle_summaries_MatchId_ProviderId_BookmakerId_Mar~",
                schema: "odds",
                table: "odds_lifecycle_summaries",
                columns: new[] { "MatchId", "ProviderId", "BookmakerId", "MarketLineId", "SelectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_odds_lifecycle_summaries_ProviderId",
                schema: "odds",
                table: "odds_lifecycle_summaries",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_odds_lifecycle_summaries_SelectionId",
                schema: "odds",
                table: "odds_lifecycle_summaries",
                column: "SelectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "odds_lifecycle_summaries",
                schema: "odds");
        }
    }
}
