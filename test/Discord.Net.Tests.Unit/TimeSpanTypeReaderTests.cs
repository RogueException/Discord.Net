using Discord.Commands;
using System;
using Xunit;

namespace Discord
{
    public class TimeSpanTypeReaderTests
    {
        [Theory]
        [InlineData("4d3h2m1s", false)]    // tests format "%d'd'%h'h'%m'm'%s's'"
        [InlineData("4d3h2m", false)]      // tests format "%d'd'%h'h'%m'm'"
        [InlineData("4d3h1s", false)]      // tests format "%d'd'%h'h'%s's'"
        [InlineData("4d3h", false)]        // tests format "%d'd'%h'h'"
        [InlineData("4d2m1s", false)]      // tests format "%d'd'%m'm'%s's'"
        [InlineData("4d2m", false)]        // tests format "%d'd'%m'm'"
        [InlineData("4d1s", false)]        // tests format "%d'd'%s's'"
        [InlineData("4d", false)]          // tests format "%d'd'"
        [InlineData("3h2m1s", false)]      // tests format "%h'h'%m'm'%s's'"
        [InlineData("3h2m", false)]        // tests format "%h'h'%m'm'"
        [InlineData("3h1s", false)]        // tests format "%h'h'%s's'"
        [InlineData("3h", false)]          // tests format "%h'h'"
        [InlineData("2m1s", false)]        // tests format "%m'm'%s's'"
        [InlineData("2m", false)]          // tests format "%m'm'"
        [InlineData("1s", false)]          // tests format "%s's'"
        // Negatives
        [InlineData("-4d3h2m1s", true)]    // tests format "-%d'd'%h'h'%m'm'%s's'"
        [InlineData("-4d3h2m", true)]      // tests format "-%d'd'%h'h'%m'm'"
        [InlineData("-4d3h1s", true)]      // tests format "-%d'd'%h'h'%s's'"
        [InlineData("-4d3h", true)]        // tests format "-%d'd'%h'h'"
        [InlineData("-4d2m1s", true)]      // tests format "-%d'd'%m'm'%s's'"
        [InlineData("-4d2m", true)]        // tests format "-%d'd'%m'm'"
        [InlineData("-4d1s", true)]        // tests format "-%d'd'%s's'"
        [InlineData("-4d", true)]          // tests format "-%d'd'"
        [InlineData("-3h2m1s", true)]      // tests format "-%h'h'%m'm'%s's'"
        [InlineData("-3h2m", true)]        // tests format "-%h'h'%m'm'"
        [InlineData("-3h1s", true)]        // tests format "-%h'h'%s's'"
        [InlineData("-3h", true)]          // tests format "-%h'h'"
        [InlineData("-2m1s", true)]        // tests format "-%m'm'%s's'"
        [InlineData("-2m", true)]          // tests format "-%m'm'"
        [InlineData("-1s", true)]          // tests format "-%s's'"
        public void TestTimeSpanParse(string input, bool isNegative)
        {
            var reader = new TimeSpanTypeReader();
            var result = reader.ReadAsync(null, input, null).Result;
            Assert.True(result.IsSuccess);

            var actual = (TimeSpan)result.BestMatch;
            Assert.True(actual != TimeSpan.Zero);

            if (isNegative)
            {
                Assert.True(actual < TimeSpan.Zero);

                Assert.True(actual.Seconds == 0 || actual.Seconds == -1);
                Assert.True(actual.Minutes == 0 || actual.Minutes == -2);
                Assert.True(actual.Hours == 0 || actual.Hours == -3);
                Assert.True(actual.Days == 0 || actual.Days == -4);
            }
            else
            {
                Assert.True(actual > TimeSpan.Zero);

                Assert.True(actual.Seconds == 0 || actual.Seconds == 1);
                Assert.True(actual.Minutes == 0 || actual.Minutes == 2);
                Assert.True(actual.Hours == 0 || actual.Hours == 3);
                Assert.True(actual.Days == 0 || actual.Days == 4);
            }
        }
    }
}
