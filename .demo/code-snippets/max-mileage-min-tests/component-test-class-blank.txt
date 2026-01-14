public class ComponentTests
{
    private ComponentTestFixture testFixture;

    [Before(Test)]
    public void Setup()
    {
        testFixture = new ComponentTestFixture();
    }

    

    [After(Test)]
    public void TearDown()
    {
        testFixture.Dispose();
    }
}