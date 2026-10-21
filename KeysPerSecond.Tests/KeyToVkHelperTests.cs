using KeysPerSecond.Services;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class KeyToVkHelperTests
    {
        [Theory]
        [InlineData("Z", 90)]
        [InlineData("z", 90)]
        [InlineData("X", 88)]
        [InlineData("C", 67)]
        [InlineData("V", 86)]
        [InlineData("A", 65)]
        [InlineData("1", 0x31)]
        [InlineData("Space", 0x20)]
        [InlineData("Spacebar", 0x20)]
        [InlineData("Shift", 0x10)]
        [InlineData("LShift", 0xA0)]
        [InlineData("RShift", 0xA1)]
        [InlineData("Ctrl", 0x11)]
        [InlineData("LCtrl", 0xA2)]
        [InlineData("RCtrl", 0xA3)]
        [InlineData("F10", 0x79)]
        [InlineData("F1", 0x70)]
        [InlineData("0x5A", 90)]
        [InlineData("88", 88)]
        public void ResolveVirtualKey_ResolvesCorrectVkCode(string input, byte expectedVk)
        {
            byte actual = KeyToVkHelper.ResolveVirtualKey(input);
            Assert.Equal(expectedVk, actual);
        }

        [Fact]
        public void ResolveVirtualKey_ReturnsZero_ForEmptyOrInvalidInput()
        {
            Assert.Equal(0, KeyToVkHelper.ResolveVirtualKey(""));
            Assert.Equal(0, KeyToVkHelper.ResolveVirtualKey("   "));
            Assert.Equal(0, KeyToVkHelper.ResolveVirtualKey("NonExistentKey12345"));
        }

        [Theory]
        [InlineData(90, "Z")]
        [InlineData(88, "X")]
        [InlineData(0x20, "SPC")]
        [InlineData(0x79, "F10")]
        public void GetDefaultLabel_ReturnsFriendlyString(byte vk, string expected)
        {
            string actual = KeyToVkHelper.GetDefaultLabel(vk);
            Assert.Equal(expected, actual);
        }
    }
}

