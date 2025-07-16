using System.Text;
using Shadow.Agent.Parsers;

namespace Shadow.Agent.UnitTests;

public class ParserTests
{
    [Fact]
    public async Task Parse_Trx_Summary_OK()
    {
        // Arrange
        var trxContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun id="12345" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <ResultSummary outcome="Completed">
                <Counters total="10" executed="10" passed="8" failed="2" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
              </ResultSummary>
            </TestRun>
            """;
        
        var parser = new TrxParser();
        
        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(trxContent));
        var result = await parser.ParseAsync(stream);
        
        // Assert
        Assert.Equal(10, result.Total);
        Assert.Equal(8, result.Passed);
        Assert.Equal(2, result.Failed);
        Assert.Equal(0, result.Skipped);
    }
    
    [Fact]
    public async Task Parse_JUnit_Summary_OK()
    {
        // Arrange
        var junitContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <testsuite name="TestSuite" tests="15" failures="3" errors="1" skipped="2" time="45.123">
              <testcase classname="MyClass" name="test1" time="1.0"/>
              <testcase classname="MyClass" name="test2" time="2.0">
                <failure message="Test failed" type="AssertionError">Stack trace here</failure>
              </testcase>
              <testcase classname="MyClass" name="test3" time="0.5">
                <skipped/>
              </testcase>
            </testsuite>
            """;
        
        var parser = new JUnitParser();
        
        // Act  
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(junitContent));
        var result = await parser.ParseAsync(stream);
        
        // Assert
        Assert.Equal(15, result.Total);
        Assert.Equal(9, result.Passed); // 15 - 3 - 1 - 2 = 9
        Assert.Equal(4, result.Failed); // failures + errors = 3 + 1
        Assert.Equal(2, result.Skipped);
    }
    
    [Fact]
    public void TrxParser_CanParse_ValidTrx()
    {
        // Arrange
        var parser = new TrxParser();
        var content = Encoding.UTF8.GetBytes(
            """<?xml version="1.0"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"></TestRun>""");
        
        // Act & Assert
        Assert.True(parser.CanParse(content));
    }
    
    [Fact]
    public void JUnitParser_CanParse_ValidJUnit()
    {
        // Arrange
        var parser = new JUnitParser();
        var content = Encoding.UTF8.GetBytes("""<testsuite name="test"></testsuite>""");
        
        // Act & Assert
        Assert.True(parser.CanParse(content));
    }
}
