using TestApp;

namespace TestAppTest
{
    public class UnitTest1
    {
        [Fact]
        public void PrintLocationSucess()
        {
            var result = 0;

            var resultTest = new PrintLocation().printLocation();

            //Assert  
            Assert.Equal(result, resultTest);
        }
    }
}