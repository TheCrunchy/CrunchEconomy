using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy.Contracts
{
    public class HaulingContract
    {
        public ContractStatus status = ContractStatus.InProgress;
        public Guid ContractId = Guid.NewGuid();
    }
}
