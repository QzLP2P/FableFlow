import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { Link as RouterLink, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/AppLayout";
import { AdventurePage } from "./pages/AdventurePage";
import { BookPage } from "./pages/BookPage";
import { CreatePage } from "./pages/CreatePage";
import { FamilyPage } from "./pages/FamilyPage";
import { HomePage } from "./pages/HomePage";
import { LibraryPage } from "./pages/LibraryPage";

function App() {
  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/create" element={<CreatePage />} />
        <Route path="/adventures/:adventureId" element={<AdventurePage />} />
        <Route path="/library" element={<LibraryPage />} />
        <Route path="/library/:bookId" element={<BookPage />} />
        <Route path="/family" element={<FamilyPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </AppLayout>
  );
}

function NotFoundPage() {
  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h1" sx={{ fontSize: "1.75rem", mb: 2 }}>
        Page introuvable
      </Typography>
      <Button variant="contained" size="large" component={RouterLink} to="/">
        Retour à l'accueil
      </Button>
    </Container>
  );
}

export default App;
