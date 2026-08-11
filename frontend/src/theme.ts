import { createTheme } from '@mui/material/styles';
import { ptBR } from '@mui/material/locale';

export const theme = createTheme(
  {
    palette: {
      primary: { main: '#0E6B58' },
      error: { main: '#8C3A52' },
      background: { default: '#EFF2EE', paper: '#FAFBF9' },
      text: { primary: '#141E19', secondary: '#55625B' },
    },
    shape: { borderRadius: 4 },
    typography: {
      fontFamily: 'system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
      h1: { fontSize: '2rem', fontWeight: 500 },
      h2: { fontSize: '1.5rem', fontWeight: 500 },
    },
    components: {
      MuiButton: {
        styleOverrides: { root: { textTransform: 'none' } },
      },
    },
  },
  ptBR,
);
