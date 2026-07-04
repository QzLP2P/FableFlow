import { Routes, Route } from 'react-router-dom';
import { HomePage } from './pages/HomePage';
import { AdventurePage } from './pages/AdventurePage';
import { AppLayout } from './components/AppLayout';

function App() {
  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/adventures/:adventureId" element={<AdventurePage />} />
      </Routes>
    </AppLayout>
  );
}

export default App;

