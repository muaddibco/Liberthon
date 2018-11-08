using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wist.Client.DataModel.Configuration;
using Wist.Client.DataModel.Model;

namespace Wist.Client.DataModel
{
    public class DataContext : DbContext
    {
        private readonly IClientDataContextConfiguration _configuration;

        public DataContext(IClientDataContextConfiguration clientDataContextConfiguration)
        {
            _configuration = clientDataContextConfiguration;
        }

        public DbSet<SyncBlock> SyncBlocks { get; set; }
        public DbSet<RegistryCombinedBlock> RegistryCombinedBlocks { get; set; }
        public DbSet<BlockHash> BlockHashes { get; set; }
        public DbSet<Identity> Identities { get; set; }
        public DbSet<TransactionalIncomingBlock> TransactionalIncomingBlocks { get; set; }
        public DbSet<TransactionalOutcomingBlock> TransactionalOutcomingBlocks { get; set; }
        public DbSet<TransactionalValidatedBlock> TransactionalValidatedBlocks { get; set; }
        public DbSet<UtxoIncomingBlock> UtxoIncomingBlocks { get; set; }
        public DbSet<UtxoOutcomingBlock> UtxoOutcomingBlocks { get; set; }
        public DbSet<UtxoKeyImage> UtxoKeyImages { get; set; }
        public DbSet<UtxoOutput> UtxoOutputs { get; set; }
        public DbSet<UtxoTransactionKey> UtxoTransactionKeys { get; set; }
        public DbSet<UtxoUnspentBlock> UtxoUnspentBlocks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_configuration.ConnectionString);
        }
    }
}
