using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Configuration;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.Txn.Application;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Miner.Application
{
    public class MinerService : IMinerService
    {
        public ILogger<MinerService> Logger { get; set; }
        private readonly ITransactionPoolService _transactionPoolService;
        private readonly TransactionPackingOptions _transactionPackingOptions;
        private readonly IMiningService _miningService;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;

        public MinerService(IMiningService miningService,
            IBlockTransactionLimitProvider blockTransactionLimitProvider,
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions, 
            ITransactionPoolService transactionPoolService)
        {
            _miningService = miningService;
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            _transactionPoolService = transactionPoolService;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;

            Logger = NullLogger<MinerService>.Instance;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <returns></returns>
        public async Task<BlockExecutedSet> MineAsync(Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime,
            Duration blockExecutionTime)
        {
            var limit = await _blockTransactionLimitProvider.GetLimitAsync(new ChainContext
            {
                BlockHash = previousBlockHash,
                BlockHeight = previousBlockHeight
            });
            var executableTransactionSet = await _transactionPoolService.GetExecutableTransactionSetAsync(
                _transactionPackingOptions.IsTransactionPackable
                    ? limit
                    : -1);
            var pending = new List<Transaction>();
            if (executableTransactionSet.PreviousBlockHash == previousBlockHash)
            {
                pending = executableTransactionSet.Transactions;
            }
            else
            {
                Logger.LogWarning($"Transaction pool gives transactions to be appended to " +
                                  $"{executableTransactionSet.PreviousBlockHash} which doesn't match the current " +
                                  $"best chain hash {previousBlockHash}.");
            }

            Logger.LogInformation(
                $"Start mining with previous hash: {previousBlockHash}, previous height: {previousBlockHeight}.");
            return await _miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = previousBlockHash,
                    PreviousBlockHeight = previousBlockHeight,
                    BlockExecutionTime = blockExecutionTime
                }, pending, blockTime);
        }
    }
}