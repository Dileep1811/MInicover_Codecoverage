using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MiniCover.Core.Model;
using MiniCover.HitServices;
using MiniCover.TestHelpers;
using MiniCover.UnitTests.TestHelpers;
using Mono.Cecil;
using Mono.Cecil.Tests;
using Xunit;

namespace MiniCover.Core.UnitTests.Instrumentation
{
    public abstract class BaseTest
    {
        private Type _typeToInstrument;
        private MethodBase _methodToInstrument;

        public BaseTest(Type typeToInstrument)
        {
            _typeToInstrument = typeToInstrument;
        }

        public BaseTest(MethodBase methodToInstrument)
        {
            _methodToInstrument = methodToInstrument;
        }

        [Fact]
        public void Test()
        {
            InstrumentedAssembly instrumentedAssembly;
            TypeDefinition typeDefinition;
            string il;

            if (_methodToInstrument != null)
            {
                var methodDefinition = _methodToInstrument.ToDefinition();
                typeDefinition = methodDefinition?.DeclaringType;
                instrumentedAssembly = methodDefinition.Instrument();
                il = new ILFormatter(false).FormatMethodBody(methodDefinition);
            }
            else
            {
                typeDefinition = _typeToInstrument.ToDefinition();
                instrumentedAssembly = typeDefinition.Instrument();
                il = new ILFormatter(false).FormatType(typeDefinition);
            }

            if (ExpectedIL != null)
            {
                il.ToOSLineEnding().Should().Be(ExpectedIL.ToOSLineEnding());
            }

            var instrumentedInstructions = instrumentedAssembly.SourceFiles
                .SelectMany(file => file.Sequences)
                .ToArray();

            if (ExpectedInstructions != null)
                instrumentedInstructions.Should().BeEquivalentTo(ExpectedInstructions, config => config
                .Using<string>(strCtx => strCtx.Subject?.ToOSLineEnding().Should().Be(strCtx.Expectation?.ToOSLineEnding()))
                .WhenTypeIs<string>());

            var instrumentedType = typeDefinition.Load();

            var instrumentedTestType = instrumentedType.Assembly.GetType(GetType().FullName);
            var functionalTestMethod = instrumentedTestType.GetMethod(nameof(FunctionalTest));

            HitContext.Current = new HitContext("Assembly", "Class", "Method");
            HitContext.Current.EnterMethod();
            var instrumentedTest = Activator.CreateInstance(instrumentedTestType);
            functionalTestMethod.Invoke(instrumentedTest, new object[0]);

            if (ExpectedHits != null)
            {
                HitContext.Current.Hits.Should().BeEquivalentTo(ExpectedHits);
            }

            if (ExpectedHitCount != null)
            {
                HitContext.Current.Hits.Sum(h => h.Value).Should().Be(ExpectedHitCount);
            }
        }

        public virtual string ExpectedIL => null;
        public virtual IDictionary<int, int> ExpectedHits => null;
        public virtual int? ExpectedHitCount => null;
        public virtual InstrumentedSequence[] ExpectedInstructions => null;

        public virtual void FunctionalTest() { }
    }
}
