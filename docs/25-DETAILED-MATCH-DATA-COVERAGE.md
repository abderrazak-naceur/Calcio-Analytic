# 25 — Detailed Match Data Coverage

## Obiettivo
Ogni partita deve essere trattata come un aggregato completo di dati: contesto, squadre, classifica storica, forma, H2H, statistiche, bookmaker, mercati, linee, selezioni, opening/current/closing odds, ogni variazione e live snapshots quando disponibili.

## Match identity
- internal match ID
- provider match IDs
- competition, season, country, round/matchday
- kickoff UTC/local timezone
- venue, referee, attendance when available
- home/away teams
- status
- half-time/full-time score
- extra time and penalties
- source provider and source timestamps

## Team context at match time
Per entrambe le squadre: position, points, matches played, wins, draws, losses, GF, GA, GD, home/away position, home/away points, streak, last 5, last 10, season form, home form, away form, clean sheets, failed to score, average goals, shots, shots on target, corners, cards e xG/xGA quando disponibili.

IMPORTANTE: classifiche e form devono essere point-in-time; non usare dati successivi alla partita.

## Historical matches
Conservare avversario, data, casa/trasferta, risultato, score, goal, corner, cards, shots, shots on target, xG, competition e strength of schedule quando disponibili.

Rolling windows: last 3, last 5, last 10, season, previous season.

## Head-to-head
Total meetings, home wins, draws, away wins, goals, average goals, BTTS rate, Over 0.5/1.5/2.5/3.5, clean sheets, last 5 e last 10 H2H.

## Full odds model
Provider → Bookmaker → Match → Market → Line → Selection → Odds Snapshot.

Non creare una colonna per ogni mercato: il modello deve essere dinamico.

## 1X2
Per ogni bookmaker: Home, Draw, Away. Conservare opening, current, closing, best, worst, average, implied probability, normalized probability, overround, absolute movement, percentage movement, first seen, last seen e number of changes.

## Goal markets
Over/Under 0.5, 1.5, 2.5, 3.5, 4.5, 5.5 e linee custom; home team totals; away team totals; goal ranges; BTTS Yes/No.

## Handicap
European handicap, Asian handicap, home/away handicap, first-half handicap e alternate handicap lines. Ogni linea è una entity separata.

## Correct score
Supportare qualsiasi score restituito dal provider, ad esempio 0-0, 1-0, 0-1, 1-1, 2-0, 2-1, 1-2, 2-2, 3-0 e così via.

## Half-time
HT 1X2, HT Over/Under, HT BTTS, HT Handicap, HT Correct Score, HT Draw No Bet e HT Team Totals.

## Combined markets
HT/FT, Double Chance, Double Chance + Over/Under, Result + BTTS, Result + Total e altre combinazioni quando disponibili.

## Goal special markets
First team to score, last team to score, no goal, odd/even goals, goal ranges, exact total goals, score in both halves, win to nil e clean sheet.

## Corners
Total corners, Over/Under corners, home corners, away corners, corner handicap, first-half corners, team corner totals, first team to get a corner e most corners.

## Cards
Total cards, Over/Under cards, home cards, away cards, card handicap, first-half cards, first team card e player cards.

## Player markets
Quando supportati: anytime scorer, first/last scorer, shots, shots on target, assists, cards, passes, tackles e altre linee player-specific.

Relazione: Player → Market → Line → Selection → Odds Snapshot.

## Live markets
Live 1X2, totals, handicap, BTTS, corners, cards, correct score e team totals quando disponibili.

Ogni live snapshot deve avere captured_at, match minute, period, score al momento dello snapshot, market, line, selection, odd e bookmaker.

## Bookmaker matrix
Per ogni partita la UI deve confrontare la stessa market/line/selection su tutti i bookmaker disponibili e indicare la best price.

## Odds history
NON salvare solo current odds. Ogni variazione deve essere uno snapshot separato con match, bookmaker, market, line, selection, captured_at e value.

Questo permette opening detection, closing detection, biggest move, fastest move, reversal, volatility, bookmaker disagreement e consensus movement.

## Opening and closing
Salvare opening timestamp/value, closing timestamp/value, total changes, maximum e minimum.

Il primo record ricevuto dal sistema non deve essere automaticamente considerato l'apertura reale: distinguere provider timestamp e ingestion timestamp.

## Timestamps
Distinguere bookmaker_update_at, provider_update_at, ingestion_at e database_inserted_at.

## Derived analytics
Per quota decimale o: implied probability = 1/o.
Per mercati multi-outcome: overround = somma delle probabilità implicite.
normalized probability = implied probability / overround.

Feature: odds delta, delta %, probability delta, market margin, rank among bookmakers, best/worst flag, consensus price, consensus probability, bookmaker deviation, movement velocity e acceleration.

## Historical snapshots
Pre-calcolare snapshot 24h, 12h, 6h, 3h, 1h, 30m, 15m, 5m e closing quando i dati disponibili lo permettono.

## Tables
matches, teams, competitions, seasons, bookmakers, markets, market_lines, selections, odds_snapshots, odds_movements, match_statistics, team_statistics, standings_snapshots, team_form_snapshots, h2h_snapshots, players, player_statistics e raw_provider_payloads.

## Odds snapshot fields
id, match_id, provider_id, bookmaker_id, market_id, market_line_id, selection_id, value_decimal, value_fractional, value_american, implied_probability, captured_at, bookmaker_updated_at, provider_updated_at, ingested_at, is_live, match_minute, period e payload_hash.

## Volume strategy
Il volume può diventare molto grande. Usare PostgreSQL partitioning, batch inserts/COPY, indici mirati, compression, retention, aggregates, object storage per raw payload e Redis per current odds.

## Provider strategy
Oddspedia dichiara pubblicamente 80+ bookmaker, 130 betting markets, aggiornamenti real-time pre-match/live e odds history nei propri feed/widgets.
Sportmonks/TXODDS dichiara 120+ bookmaker, 42 mercati, aggiornamento pre-match circa ogni minuto e storico di opening + cambi fino a 7 giorni dopo il kickoff.
Sportradar documenta mercati come 1X2, total, spread, handicap, BTTS, correct score, HT/FT, first-half 1X2, double chance e draw-no-bet.

## Scraping policy
Un eventuale ScraperAdapter deve essere usato solo per fonti che consentono accesso automatizzato e riutilizzo. Deve includere rate limiting, caching, retry/backoff, attribution quando richiesta e configurazione provider-specifica.

Non costruire il business su scraping non autorizzato.

## Target Match Detail
Tab principali: Overview, 1X2, Goals, Handicap, Correct Score, Half Time, BTTS, Corners, Cards, Players, Live, Odds Movement, Bookmakers, Team Form, Standings, H2H, Statistics, Models e Data Quality.

## Final target
Una singola partita deve permettere di esplorare contesto, squadre, standings, form, H2H, statistics, tutti i bookmaker disponibili, tutti i mercati disponibili, opening/current/closing, odds history, movements, best price, margin, analytics, models e provenance.

Il mercato effettivamente disponibile dipende da provider, competizione, bookmaker, paese, momento della partita e licenza.