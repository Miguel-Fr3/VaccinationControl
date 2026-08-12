import { describe, expect, it } from 'vitest';

import { formatCpf, isValidCpf, searchTerm, stripCpf } from './cpf';

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

describe('isValidCpf', () => {
  it('aceita CPF com os verificadores corretos', () => {
    expect(isValidCpf('11144477735')).toBe(true);
    expect(isValidCpf('52998224725')).toBe(true);
  });

  it('aceita o CPF mascarado, como sai do campo', () => {
    expect(isValidCpf('111.444.777-35')).toBe(true);
  });

  it('aceita CPF que comeca com zero', () => {
    // O primeiro dígito não pode se perder no caminho.
    expect(isValidCpf('01234567890')).toBe(true);
  });

  it('recusa verificador trocado', () => {
    expect(isValidCpf('11144477736')).toBe(false);
    expect(isValidCpf('12345678901')).toBe(false);
  });

  it('recusa sequencia de digito repetido', () => {
    // Ela fecha a aritmética e não é CPF de ninguém.
    expect(isValidCpf('11111111111')).toBe(false);
    expect(isValidCpf('00000000000')).toBe(false);
  });

  it('recusa o que nao tem onze digitos', () => {
    expect(isValidCpf('')).toBe(false);
    expect(isValidCpf('1114447773')).toBe(false);
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
