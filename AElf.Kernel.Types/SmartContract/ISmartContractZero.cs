using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Types;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {        
        string GetContractInfo(Address address);
        byte[] DeploySmartContract(int category, byte[] code);
    }
}