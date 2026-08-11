import { Container, Typography } from '@mui/material';
import { Route, Routes } from 'react-router-dom';

function Home() {
  return (
    <Container maxWidth="md" sx={{ py: 6 }}>
      <Typography variant="h1">Cartão de Vacinação</Typography>
    </Container>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
    </Routes>
  );
}
