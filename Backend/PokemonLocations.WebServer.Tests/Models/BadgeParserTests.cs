using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Tests.Models;

public class BadgeParserTests {
    [Theory]
    [InlineData("boulder", Badge.Boulder)]
    [InlineData("cascade", Badge.Cascade)]
    [InlineData("thunder", Badge.Thunder)]
    [InlineData("rainbow", Badge.Rainbow)]
    [InlineData("soul", Badge.Soul)]
    [InlineData("marsh", Badge.Marsh)]
    [InlineData("volcano", Badge.Volcano)]
    [InlineData("earth", Badge.Earth)]
    public void TryParseAcceptsKnownBadges(string input, Badge expected) {
        Assert.True(BadgeParser.TryParse(input, out var badge));
        Assert.Equal(expected, badge);
    }

    [Theory]
    [InlineData("Boulder")]
    [InlineData("BOULDER")]
    [InlineData("BoUlDeR")]
    public void TryParseIsCaseInsensitive(string input) {
        Assert.True(BadgeParser.TryParse(input, out var badge));
        Assert.Equal(Badge.Boulder, badge);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notreal")]
    [InlineData("rock")]
    [InlineData("1")]
    public void TryParseRejectsUnknownInput(string input) {
        Assert.False(BadgeParser.TryParse(input, out _));
    }

    [Fact]
    public void TryParseRejectsNull() {
        Assert.False(BadgeParser.TryParse(null, out _));
    }

    [Fact]
    public void ToWireFormatReturnsLowercaseName() {
        Assert.Equal("boulder", BadgeParser.ToWireFormat(Badge.Boulder));
        Assert.Equal("earth", BadgeParser.ToWireFormat(Badge.Earth));
    }
}
