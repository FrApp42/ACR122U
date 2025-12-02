using FrApp42.ACR122U;

namespace ACR122U.Test
{
    [TestClass]
    public sealed class ReaderTest
    {
        [TestMethod]
        public void ReadBinaryTest()
        {
            var reader = new Reader();
            var tcs = new TaskCompletionSource<string>();

            reader.Inserted += uid =>
            {
                tcs.SetResult(uid);
            };

            bool eventTriggered = tcs.Task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(eventTriggered, "The Inserted event was not triggered");

            // Lecture des données après l'insertion
            byte[] value = null;
            try
            {
                value = reader.ReadBinary(1, 16);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

            Assert.IsNotNull(value, "The data read is zero.");
            Assert.IsNotEmpty(value, "The data read is empty.");

            Assert.IsFalse(string.IsNullOrEmpty(tcs.Task.Result), "UID is empty");
        }

        [TestMethod]
        public void WriteBinaryTest()
        {
            byte[] value = new byte[] { 0x49, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56 };

            var reader = new Reader();
            var tcs = new TaskCompletionSource<string>();

            reader.Inserted += uid =>
            {
                tcs.SetResult(uid);
            };

            bool eventTriggered = tcs.Task.Wait(TimeSpan.FromSeconds(10));
            Assert.IsTrue(eventTriggered, "The Inserted event was not triggered");

            bool result = false;
            try
            {
                result = reader.WriteBinary(8, value);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

            Assert.IsTrue(result, "Data writing failed");

            byte[] data = null;
            try
            {
                data = reader.ReadBinary(8, value.Length);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

            CollectionAssert.AreEqual(value, data, "The data read is different from the data written.");
        }
    }
}
