// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-02 오전 1:06:32   
// @PURPOSE     : 1. ReadWriteLockSlim의 언락을 편하게 하도록하자
//                2. 코드 더럽게 하는거 방지                  
// @REFERENCE   : https://theburningmonk.com/2010/02/threading-using-readerwriterlockslim/
// ===============================


using System;
using System.Threading;

namespace Shared.Util
{
    public static class ReadWriteLockSlimExtension
    {
        private sealed class ReadLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public ReadLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterReadLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitReadLock();
                    _sync = null;
                }
            }
        }
        private sealed class WriteLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;
            public WriteLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterWriteLock();
            }
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitWriteLock();
                    _sync = null;
                }
            }
        }

        public static IDisposable Read(this ReaderWriterLockSlim obj)
        {
            return new ReadLockToken(obj);
        }
        public static IDisposable Write(this ReaderWriterLockSlim obj)
        {
            return new WriteLockToken(obj);
        }
    }
}
