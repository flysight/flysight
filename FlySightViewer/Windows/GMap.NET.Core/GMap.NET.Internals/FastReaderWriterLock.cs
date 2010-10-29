
namespace GMap.NET.Internals
{
   using System;
   using System.Threading;

   /// <summary>
   /// custom ReaderWriterLock
   /// in Vista and later uses integrated Slim Reader/Writer (SRW) Lock
   /// http://msdn.microsoft.com/en-us/library/aa904937(VS.85).aspx
   /// http://msdn.microsoft.com/en-us/magazine/cc163405.aspx#S2
   /// </summary>
   public sealed class FastReaderWriterLock
   {
      static readonly bool VistaOrLater = Stuff.IsRunningOnVistaOrLater();
      Int32 busy = 0;
      Int32 readCount = 0;

      public void AcquireReaderLock()
      {
         {
            Thread.BeginCriticalRegion();

            while(Interlocked.CompareExchange(ref busy, 1, 0) != 0)
            {
               Thread.Sleep(1);
            }

            Interlocked.Increment(ref readCount);

            // somehow this fix deadlock on heavy reads
            Thread.Sleep(0);
            Thread.Sleep(0);
            Thread.Sleep(0);
            Thread.Sleep(0);
            Thread.Sleep(0);
            Thread.Sleep(0);
            Thread.Sleep(0);

            Interlocked.Exchange(ref busy, 0);
         }
      }

      public void ReleaseReaderLock()
      {
         {
            Interlocked.Decrement(ref readCount);
            Thread.EndCriticalRegion();
         }
      }

      public void AcquireWriterLock()
      {
         {
            Thread.BeginCriticalRegion();

            while(Interlocked.CompareExchange(ref busy, 1, 0) != 0)
            {
               Thread.Sleep(1);
            }

            while(Interlocked.CompareExchange(ref readCount, 0, 0) != 0)
            {
               Thread.Sleep(1);
            }
         }
      }

      public void ReleaseWriterLock()
      {
         {
            Interlocked.Exchange(ref busy, 0);
            Thread.EndCriticalRegion();
         }
      }
   }
}
