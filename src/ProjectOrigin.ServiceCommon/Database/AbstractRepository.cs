using System;
using System.Data;

namespace ProjectOrigin.ServiceCommon.Database;

public abstract class AbstractRepository
{
    private readonly IDbTransaction _transaction;

    protected IDbConnection Connection => this._transaction?.Connection
        ?? throw new InvalidOperationException("Transaction is closed and no longer valid.");

    protected AbstractRepository(IDbTransaction transaction) => this._transaction = transaction;
}
