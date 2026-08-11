using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace DynamicNotch.Services
{
    /// <summary>
    /// Provides spring-physics-based animation helpers.
    /// Uses a custom easing function that simulates damped spring motion.
    /// </summary>
    public class SpringEasingFunction : EasingFunctionBase
    {
        // Spring parameters
        public double Stiffness { get; set; } = 300;
        public double Damping { get; set; } = 25;
        public double Mass { get; set; } = 1.0;

        protected override double EaseInCore(double normalizedTime)
        {
            // Damped spring simulation
            double t = normalizedTime;

            double omega0 = Math.Sqrt(Stiffness / Mass);
            double zeta = Damping / (2 * Math.Sqrt(Stiffness * Mass));

            if (zeta >= 1.0)
            {
                // Overdamped - smooth but no bounce
                double alpha = omega0 * zeta;
                double beta = omega0 * Math.Sqrt(zeta * zeta - 1);
                double r1 = -alpha + beta;
                double r2 = -alpha - beta;
                double c2 = -r1 / (r2 - r1);
                double c1 = 1 - c2;
                return 1 - (c1 * Math.Exp(r1 * t * 1.5) + c2 * Math.Exp(r2 * t * 1.5));
            }
            else
            {
                // Underdamped - has slight overshoot/bounce
                double omegaD = omega0 * Math.Sqrt(1 - zeta * zeta);
                double envelope = Math.Exp(-zeta * omega0 * t * 1.2);
                double oscillation = Math.Cos(omegaD * t * 1.2)
                                   + (zeta * omega0 / omegaD) * Math.Sin(omegaD * t * 1.2);
                return 1 - envelope * oscillation;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new SpringEasingFunction
            {
                Stiffness = Stiffness,
                Damping = Damping,
                Mass = Mass
            };
        }
    }

    /// <summary>
    /// Helper to build spring-animated storyboards programmatically.
    /// </summary>
    public static class SpringAnimator
    {
        /// <summary>
        /// Creates a spring-physics DoubleAnimation for a dependency property.
        /// </summary>
        public static DoubleAnimation CreateSpringAnimation(
            double from,
            double to,
            TimeSpan duration,
            double stiffness = 300,
            double damping = 22,
            double mass = 1.0)
        {
            var easing = new SpringEasingFunction
            {
                Stiffness = stiffness,
                Damping = damping,
                Mass = mass,
                EasingMode = EasingMode.EaseOut
            };

            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            };
        }

        /// <summary>
        /// Creates a spring opacity animation (for content fade in/out).
        /// </summary>
        public static DoubleAnimation CreateOpacitySpring(
            double from,
            double to,
            TimeSpan duration,
            double damping = 30)
        {
            var easing = new SpringEasingFunction
            {
                Stiffness = 400,
                Damping = damping,
                Mass = 1.0,
                EasingMode = EasingMode.EaseOut
            };

            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            };
        }
    }
}