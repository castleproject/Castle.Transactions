// Copyright 2004-2009 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Services.Transaction
{
	using System;
	using System.Collections;


	/// <summary>
	/// Implements a transaction root.
	/// </summary>
	public class StandardTransaction : TransactionBase
	{
		private readonly TransactionDelegate onTransactionCommitted;
		private readonly TransactionDelegate onTransactionRolledback;
		private readonly TransactionErrorDelegate onTransactionFailed;

		private IList children = ArrayList.Synchronized( new ArrayList() );
		private bool rollbackOnly;

		protected StandardTransaction()
		{
		}

		public StandardTransaction(TransactionDelegate onTransactionCommitted, TransactionDelegate onTransactionRolledback,
			TransactionErrorDelegate onTransactionFailed, TransactionMode transactionMode, IsolationMode isolationMode, bool distributedTransaction) : 
			this(transactionMode, isolationMode, distributedTransaction)
		{
			this.onTransactionCommitted = onTransactionCommitted;
			this.onTransactionRolledback = onTransactionRolledback;
			this.onTransactionFailed = onTransactionFailed;
		}

		public StandardTransaction(TransactionMode transactionMode, IsolationMode isolationMode, bool distributedTransaction) : 
			base(transactionMode, isolationMode, distributedTransaction)
		{
		}

		public StandardTransaction CreateChildTransaction()
		{
			ChildTransaction child = new ChildTransaction(this);
			
			children.Add(child);

			return child;
		}

		public override bool IsChildTransaction
		{
			get { return false; }
		}

		public override void Commit()
		{
			if (rollbackOnly)
			{
				throw new TransactionException("Can't commit as transaction was marked as 'rollback only'");
			}

			try
			{
				base.Commit();
			}
			catch (TransactionException transactionError)
			{
				if (onTransactionFailed != null)
				{
					try
					{
						onTransactionFailed(this, transactionError);
					}
					catch (Exception e)
					{
						// Log exception & swallow it
						Logger.Warn("An error occurred while notifying caller about transaction commit failure.", e);
					}
				}
				throw;
			}

			if (onTransactionCommitted != null) onTransactionCommitted(this);
		}

		public override void Rollback()
		{
			try
			{
				base.Rollback();
			}
			catch (TransactionException ex)
			{
				if (onTransactionFailed != null)
				{
					try
					{
						onTransactionFailed(this, ex);
					}
					catch (Exception e)
					{
						// Log exception & swallow it
						Logger.Warn("An error occurred while notifying caller about transaction rollback failure.", e);
					}
				}
				throw;
			}

			if (onTransactionRolledback != null) onTransactionRolledback(this);
		}

		public override void SetRollbackOnly()
		{
			rollbackOnly = true;
		}

		public override bool IsRollbackOnlySet
		{
			get { return rollbackOnly; }
		}
	}
}
