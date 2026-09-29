import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import Dashboard from './pages/Dashboard'
import MatchList from './pages/MatchList'
import MatchDetail from './pages/MatchDetail'
import PatternExplorer from './pages/PatternExplorer'
import SimilarMatches from './pages/SimilarMatches'

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
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
