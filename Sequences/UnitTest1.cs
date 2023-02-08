using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using NUnit.Framework;

namespace Sequences
{
    public static class EnumerableExtensions
    {
        public static TSource WithMinimum<TSource,TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> criteria)
        where TSource : class 
        where TResult : IComparable<TResult>
        {
            return enumerable
                .Aggregate(
                    (TSource)null,
                    (best, current) =>
                        best == null || criteria(current).CompareTo(criteria(best)) < 0  ? current : best
                );
        }
    }
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var proportionalPainters = new ProportionalPainter[10];
            var cheapestPainter = PainterFactory.CheapestPainter(proportionalPainters);

            Assert.Pass();
        }

        public IPainter FindCheapestPainter(double sqMeters, Painters painters)
        {
            return painters.GetIsAvailable().GetCheapestOne(sqMeters);
        }

        public IPainter FindFastestPainter(double sqMeters, Painters painters)
        {
            return painters.GetIsAvailable().GetFasterOne(sqMeters);
        }

        public IPainter WorkTogether(double sqMeters, IEnumerable<IPainter> painters)
        {
            var time = TimeSpan.FromHours(1/painters.Where(w=>w.IsAvailable).Sum(x=> 1 / x.EstimateTimeToPaint(sqMeters).TotalHours));
            var cost = painters.Where(w=>w.IsAvailable).Sum(x =>
                x.EstimateCompensation(sqMeters) / x.EstimateTimeToPaint(sqMeters).TotalHours * time.TotalHours);
            return ProportionalPainter.Create(time, cost, sqMeters);
        }
    }

    public class ProportionalPainter : IPainter
    {
        public ProportionalPainter(TimeSpan timePerSqMeter, double dollarPerHour)
        {
            TimePerSqMeter = timePerSqMeter;
            DollarPerHour = dollarPerHour;
        }
        public static ProportionalPainter Create(TimeSpan totalTime, double totalCost, double sqMeters )
        {
            var timePerSqMeter = TimeSpan.FromHours(totalTime.TotalHours / sqMeters);
            var dollarPerHour = totalCost / totalTime.TotalHours;
            return new ProportionalPainter(timePerSqMeter, dollarPerHour);
        }
        public bool IsAvailable => true;
        public TimeSpan TimePerSqMeter { get; }
        public double DollarPerHour { get;}
        public TimeSpan EstimateTimeToPaint(double sqMeters)
        {
            return TimeSpan.FromHours(sqMeters * TimePerSqMeter.Hours);
        }
        public double EstimateCompensation(double sqMeters)
        {
            return EstimateTimeToPaint(sqMeters).Hours * DollarPerHour;
        }
    }

    public static class PainterFactory
    {
        public static IPainter CompositePainter(IEnumerable<ProportionalPainter> painters)
        {
            return new CompositePainter<ProportionalPainter>(painters,(sqMeters, seqence) =>
            {
                var time = TimeSpan.FromHours(1/seqence.Where(w=>w.IsAvailable).Sum(x=> 1 / x.EstimateTimeToPaint(sqMeters).TotalHours));
                var cost = seqence.Where(w=>w.IsAvailable).Sum(x =>
                    x.EstimateCompensation(sqMeters) / x.EstimateTimeToPaint(sqMeters).TotalHours * time.TotalHours);
                return ProportionalPainter.Create(time, cost, sqMeters);
            } );
        }
        public static IPainter FastestPainter(IEnumerable<IPainter> painters)
        {
            return new CompositePainter<IPainter>(painters, (sqMeters, seqenence) => new Painters(seqenence).GetFasterOne(sqMeters));
        }
        public static IPainter CheapestPainter(IEnumerable<IPainter> painters)
        {
            return new CompositePainter<IPainter>(painters, (sqMeters, seqenence) => new Painters(seqenence).GetCheapestOne(sqMeters));
        }
    }
    public class CompositePainter<TPainter> : IPainter
    where TPainter : IPainter
    {
        private IEnumerable<TPainter> Painters { get; }

        public CompositePainter(IEnumerable<TPainter> painters, Func<double, IEnumerable<TPainter>, TPainter> reduce)
        {
            Painters = painters;
            Reduce = reduce;
        }
        public bool IsAvailable => Painters.Any(x => x.IsAvailable);
        public TimeSpan EstimateTimeToPaint(double sqMeters)
        {
            return Reduce(sqMeters,Painters).EstimateTimeToPaint(sqMeters);
        }
        public double EstimateCompensation(double sqMeters)
        {
            return Reduce(sqMeters,Painters).EstimateCompensation(sqMeters);
        }
        // public IPainter Reduce(double sqMeters, IEnumerable<IPainter> painters)
        // {
        //     var time = TimeSpan.FromHours(1/painters.Where(w=>w.IsAvailable).Sum(x=> 1 / x.EstimateTimeToPaint(sqMeters).TotalHours));
        //     var cost = painters.Where(w=>w.IsAvailable).Sum(x =>
        //         x.EstimateCompensation(sqMeters) / x.EstimateTimeToPaint(sqMeters).TotalHours * time.TotalHours);
        //     return new ProportionalPainter()
        //     {
        //         TimePerSqMeter = TimeSpan.FromHours(time.TotalHours / sqMeters),
        //         DollarPerHour = cost / time.TotalHours
        //     };
        // }

        public Func<double,IEnumerable<TPainter>,TPainter> Reduce { get; set; }
    }
    
    public class Painters : IEnumerable<IPainter>
    {
        private IEnumerable<IPainter> ContainPainters { get; }

        public Painters(IEnumerable<IPainter> containPainters)
        {
            ContainPainters = containPainters;
        }
        public IEnumerator<IPainter> GetEnumerator()
        {
            return ContainPainters.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public Painters GetIsAvailable()
        {
            return new Painters(this.Where(w => w.IsAvailable));
        }

        public IPainter GetCheapestOne(double sqMeters)
        {
            return this.WithMinimum(o=>o.EstimateCompensation(sqMeters));
        }

        public IPainter GetFasterOne(double sqMeters)
        {
            return this.WithMinimum(o=>o.EstimateTimeToPaint(sqMeters));
        }
    }
}