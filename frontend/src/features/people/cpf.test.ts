import { describe, expect, it } from 'vitest';

import { formatCpf, searchTerm, stripCpf } from './cpf';

describe('stripCpf', () => {
  it('deixa so os digitos', () => {
    expect(stripCpf('123.456.789-01')).toBe('12345678901');
  });

  it('corta o que passa de 11 digitos', () => {
    // O campo aceita a digitação, mas a API recusa qualquer coisa fora dos 11.
    expect(stripCpf('123456789012345')).toBe('12345678901');
  });

  it('devolve vazio quando nao ha digito', () => {
    expect(stripCpf('...--')).toBe('');
  });
});

describe('formatCpf', () => {
  it('mascara o CPF completo', () => {
    expect(formatCpf('12345678901')).toBe('123.456.789-01');
  });

  it('mascara o valor parcial enquanto se digita', () => {
    expect(formatCpf('123')).toBe('123');
    expect(formatCpf('1234')).toBe('123.4');
    expect(formatCpf('1234567')).toBe('123.456.7');
    expect(formatCpf('123456789')).toBe('123.456.789');
    expect(formatCpf('1234567890')).toBe('123.456.789-0');
  });

  it('nao duplica separador em valor ja mascarado', () => {
    expect(formatCpf('123.456.789-01')).toBe('123.456.789-01');
  });
});

describe('searchTerm', () => {
  it('limpa a pontuacao quando o termo parece CPF', () => {
    expect(searchTerm('123.456')).toBe('123456');
  });

  it('preserva a busca por nome', () => {
    // Limpar aqui devolveria string vazia e a busca por nome pararia de funcionar.
    expect(searchTerm('Maria Silva')).toBe('Maria Silva');
  });

  it('preserva nome com numero', () => {
    expect(searchTerm('Maria 2')).toBe('Maria 2');
  });
});
