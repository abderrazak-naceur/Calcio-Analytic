import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import Dashboard from './pages/Dashboard'
import MatchList from './pages/MatchList'
import MatchDetail from './pages/MatchDetail'
import PatternExplorer from './pages/PatternExplorer'
import SimilarMatches from './pages/SimilarMatches'
import OddsMovementExplorer from './pages/OddsMovementExplorer'
import BookmakerComparison from './pages/BookmakerComparison'
import HighOddsIntelligence from './pages/HighOddsIntelligence'
import BacktestingLab from './pages/BacktestingLab'
import MarketOutcomes from './pages/MarketOutcomes'
import UpcomingAnalysis from './pages/UpcomingAnalysis'
import Upsets from './pages/Upsets'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/matches" element={<MatchList />} />
          <Route path="/matches/:id" element={<MatchDetail />} />
          <Route path="/matches/:id/similar" element={<SimilarMatches />} />
          <Route path="/patterns" element={<PatternExplorer />} />
          <Route path="/movement" element={<OddsMovementExplorer />} />
          <Route path="/bookmakers" element={<BookmakerComparison />} />
          <Route path="/high-odds" element={<HighOddsIntelligence />} />
          <Route path="/market-outcomes" element={<MarketOutcomes />} />
          <Route path="/upcoming" element={<UpcomingAnalysis />} />
          <Route path="/upsets" element={<Upsets />} />
          <Route path="/backtesting" element={<BacktestingLab />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
