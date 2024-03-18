using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maurer.OktaFilter.Services;
using UnitTesting.Fixture;

namespace UnitTesting.Harness
{
    public class OktaServiceHarness : AbstractHarness<OktaServiceFixture>
    {
        protected override async Task<object?> Act(params object[] parameters)
        {
            TokenService service = (TokenService) parameters[0];

            return await service.GetToken();
        }

        public OktaServiceHarness (OktaServiceFixture filter) : base (filter) 
        { 
            
        }
    }
}
