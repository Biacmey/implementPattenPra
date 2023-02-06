using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using NUnit.Framework;

namespace Sequences
{
    public static class EnumerableExtension
    {
        public static TSource WithMin<TSource,TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> keySelector)
        where TSource : class
        where TResult : IComparable<TResult>
        {
            return enumerable
                .Aggregate((TSource)null
                    ,(best,current)=>best == null || keySelector(best).CompareTo(keySelector(current)) > 0
                        ? current 
                        : best);
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
            var cheapestPainter = PainterFactory.CompositePainter(proportionalPainters);
            Assert.Pass();
        }
        public IPainter FindCheapestPainter(double sqMeters, Painters painters)
        {
            var painter = painters.GetAvailable().GetCheaperOne(sqMeters);
            return null;
        }
        public IPainter FindFastestPainter(double sqMeters, Painters painters)
        {
            var enumerable = painters.GetAvailable().GetFatestOne(sqMeters);
            return null;
        }

        public IPainter workTogether(double sqMeters, IEnumerable<IPainter> painters)
        {
            var time = TimeSpan.FromHours(1/painters.Where(w=>w.IsAvailable).Sum(x=> 1 / x.EstimateTimeToPaint(sqMeters).TotalHours));
            var cost = painters.Where(w=>w.IsAvailable).Sum(x =>
                x.EstimateCompensation(sqMeters) / x.EstimateTimeToPaint(sqMeters).TotalHours * time.TotalHours);
            return ProportionalPainter.Create(time,cost,sqMeters);
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
        public TimeSpan TimePerSqMeter { get;}
        public double DollarPerHour { get;}
        public bool IsAvailable => true;
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
            return new CompositePainter<ProportionalPainter>(painters, (sqMeters, seqence) =>
            {
                var time = TimeSpan.FromHours(1/seqence.Where(w=>w.IsAvailable).Sum(x=> 1 / x.EstimateTimeToPaint(sqMeters).TotalHours));
                var cost = seqence.Where(w=>w.IsAvailable).Sum(x =>
                    x.EstimateCompensation(sqMeters) / x.EstimateTimeToPaint(sqMeters).TotalHours * time.TotalHours);
                return ProportionalPainter.Create(time,cost,sqMeters);
            });
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

        private Func<double,IEnumerable<TPainter>,TPainter> Reduce { get;}
    }
    public class Painters : IEnumerable<IPainter>
    {
        public Painters(IEnumerable<IPainter> containPainters)
        {
            ContainPainters = containPainters;
        }
        private IEnumerable<IPainter> ContainPainters { get; }
        public IEnumerator<IPainter> GetEnumerator()
        {
            return ContainPainters.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public Painters GetAvailable()
        {
            return new Painters(this.Where(w => w.IsAvailable));
        }
        public IPainter GetCheaperOne(double sqMeters)
        {
            return this.WithMin(o => o.EstimateCompensation(sqMeters));
        }

        public IPainter GetFatestOne(double sqMeters)
        {
            return this.WithMin(o => o.EstimateTimeToPaint(sqMeters));
        }
    }
}