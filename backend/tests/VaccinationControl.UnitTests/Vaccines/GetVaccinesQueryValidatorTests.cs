using FluentValidation.TestHelper;
using VaccinationControl.Application.Vaccines.Queries.GetVaccines;

namespace VaccinationControl.UnitTests.Vaccines
{
    public class GetVaccinesQueryValidatorTests
    {
        private readonly GetVaccinesQueryValidator _validator = new();

        [Fact]
        public void Deve_aceitar_consulta_sem_nenhum_parametro()
        {
            var result = _validator.TestValidate(new GetVaccinesQuery());

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Deve_recusar_pagina_menor_que_um(int page)
        {
            var result = _validator.TestValidate(new GetVaccinesQuery(Page: page));

            result.ShouldHaveValidationErrorFor(query => query.Page);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Deve_recusar_pageSize_fora_de_1_a_100(int pageSize)
        {
            var result = _validator.TestValidate(new GetVaccinesQuery(PageSize: pageSize));

            result.ShouldHaveValidationErrorFor(query => query.PageSize);
        }

        [Fact]
        public void Deve_aceitar_pageSize_no_teto_de_100()
        {
            var result = _validator.TestValidate(new GetVaccinesQuery(PageSize: 100));

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Deve_recusar_busca_acima_de_200_caracteres()
        {
            var query = new GetVaccinesQuery(Search: new string('a', 201));

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(query => query.Search);
        }
    }
}
