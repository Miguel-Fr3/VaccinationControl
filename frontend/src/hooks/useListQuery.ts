import { useState } from 'react';

import type { ListQuery } from '../api/types';
import { useDebouncedValue } from './useDebouncedValue';

/** Estado de busca e paginação de uma listagem, mais os parâmetros prontos para a API. */
export function useListQuery(initialPageSize = 20) {
  const [search, setSearchValue] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSizeValue] = useState(initialPageSize);

  const debouncedSearch = useDebouncedValue(search);

  // Filtrar e trocar o tamanho da página voltam para a primeira: a página 3 do estado
  // anterior pode não existir no novo, e a listagem viria vazia sem explicação.
  const setSearch = (value: string) => {
    setSearchValue(value);
    setPage(1);
  };

  const setPageSize = (value: number) => {
    setPageSizeValue(value);
    setPage(1);
  };

  const query: ListQuery = { search: debouncedSearch || undefined, page, pageSize };

  return { search, page, pageSize, query, setSearch, setPage, setPageSize };
}
