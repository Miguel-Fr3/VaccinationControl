import { useEffect, useState } from 'react';

// Atrasa o valor até ele parar de mudar por `delay` ms. Uma requisição por pausa, não por tecla.
export function useDebouncedValue<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebounced(value);
    }, delay);

    // Cada tecla cancela o temporizador anterior.
    return () => {
      clearTimeout(timer);
    };
  }, [value, delay]);

  return debounced;
}
