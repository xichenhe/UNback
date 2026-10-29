using NUnit.Framework;
using ToolNBack;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The single-item mental-effort rating asked after every block. The same face
    /// button answers the n-back and confirms the rating, so a press carried over
    /// from the last trial must never become a rating.
    /// </summary>
    public class EffortProbeTests
    {
        private const float Timeout = 20f;

        [Test]
        public void NoNumberIsSelected_UntilTheStickMoves()
        {
            var probe = new EffortProbe(1, 9, Timeout);

            probe.Tick(2f);

            Assert.IsNull(probe.Selection, "starting on a number would anchor ratings to it");
            Assert.IsFalse(probe.Confirm(), "a press with nothing selected must not answer the probe");
            Assert.IsFalse(probe.IsFinished);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void FirstMove_SelectsTheMiddleOfTheScale(int direction)
        {
            var probe = new EffortProbe(1, 9, Timeout);

            probe.Move(direction);

            Assert.AreEqual(5, probe.Selection);
        }

        [Test]
        public void LaterMoves_StepByOne_AndStopAtTheEnds()
        {
            var probe = new EffortProbe(1, 9, Timeout);
            probe.Move(1); // 5

            probe.Move(1);
            Assert.AreEqual(6, probe.Selection);

            for (int i = 0; i < 10; i++)
                probe.Move(1);
            Assert.AreEqual(9, probe.Selection, "the selection must stop at the top of the scale");

            for (int i = 0; i < 20; i++)
                probe.Move(-1);
            Assert.AreEqual(1, probe.Selection, "the selection must stop at the bottom of the scale");
        }

        [Test]
        public void Confirm_IsIgnoredRightAfterTheScaleAppears()
        {
            var probe = new EffortProbe(1, 9, Timeout);
            probe.Move(1);

            probe.Tick(EffortProbe.LockoutSeconds * 0.5f);
            Assert.IsFalse(probe.Confirm(), "a press within the lockout is a leftover n-back response");

            probe.Tick(EffortProbe.LockoutSeconds);
            Assert.IsTrue(probe.Confirm());
            Assert.AreEqual(5, probe.Rating);
            Assert.AreEqual(EffortProbe.LockoutSeconds * 1.5f, probe.ResponseTime, 1e-4f,
                "response time runs from the moment the scale appeared");
            Assert.IsTrue(probe.IsFinished);
        }

        [Test]
        public void Timeout_FinishesWithoutARating()
        {
            var probe = new EffortProbe(1, 9, Timeout);
            probe.Move(1); // a selection without a confirm is not an answer

            probe.Tick(Timeout + 0.1f);

            Assert.IsTrue(probe.TimedOut);
            Assert.IsTrue(probe.IsFinished);
            Assert.IsNull(probe.Rating);
            Assert.Less(probe.ResponseTime, 0f, "no response, no response time");
        }

        [Test]
        public void ZeroTimeout_WaitsForTheAnswer()
        {
            var probe = new EffortProbe(1, 9, timeoutSeconds: 0f);

            probe.Tick(10000f);

            Assert.IsFalse(probe.IsFinished);
        }

        [Test]
        public void AfterTheAnswer_FurtherInputChangesNothing()
        {
            var probe = new EffortProbe(1, 9, Timeout);
            probe.Move(1);
            probe.Tick(1f);
            probe.Confirm();

            probe.Move(1);
            probe.Tick(Timeout);

            Assert.AreEqual(5, probe.Rating);
            Assert.AreEqual(5, probe.Selection);
            Assert.IsFalse(probe.TimedOut, "an answered probe cannot time out afterwards");
            Assert.IsFalse(probe.Confirm(), "the probe answers only once");
        }

        // --- digit keys (the experimenter types the number the participant says) ----

        [Test]
        public void Choose_AnswersAtOnce_AfterTheLockout()
        {
            var probe = new EffortProbe(1, 9, Timeout);
            probe.Tick(1f);

            Assert.IsTrue(probe.Choose(7));

            Assert.AreEqual(7, probe.Rating);
            Assert.AreEqual(1f, probe.ResponseTime, 1e-4f);
        }

        [Test]
        public void Choose_DuringTheLockout_OnlySelectsTheNumber()
        {
            var probe = new EffortProbe(1, 9, Timeout);

            Assert.IsFalse(probe.Choose(7));

            Assert.AreEqual(7, probe.Selection, "the number is kept, so a later confirm answers it");
            Assert.IsNull(probe.Rating);
        }

        [Test]
        public void Choose_OutsideTheScale_IsIgnored()
        {
            var probe = new EffortProbe(1, 7, Timeout);
            probe.Tick(1f);

            Assert.IsFalse(probe.Choose(9));
            Assert.IsFalse(probe.Choose(0));

            Assert.IsNull(probe.Selection);
            Assert.IsNull(probe.Rating);
        }

        // --- thumbstick steps ------------------------------------------------------

        [Test]
        public void Stick_StepsOncePerPush_NotEveryFrameItIsHeld()
        {
            var stepper = new StickStepper();

            Assert.AreEqual(1, stepper.Step(0.9f));
            Assert.AreEqual(0, stepper.Step(0.95f), "holding the stick must not keep stepping");
            Assert.AreEqual(0, stepper.Step(0.1f), "returning to the centre releases it");
            Assert.AreEqual(1, stepper.Step(0.8f), "a second push steps again");
        }

        [Test]
        public void Stick_IgnoresSmallWobble()
        {
            var stepper = new StickStepper();

            Assert.AreEqual(0, stepper.Step(0.4f));
            Assert.AreEqual(0, stepper.Step(-0.4f));
        }

        [Test]
        public void Stick_SwungStraightAcross_StepsTheOtherWay()
        {
            var stepper = new StickStepper();
            stepper.Step(0.9f);

            Assert.AreEqual(-1, stepper.Step(-0.9f),
                "a fast swing can skip the centre between two frames");
        }
    }
}
