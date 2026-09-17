namespace Tickflo.CoreTest.Services.Widgets;

using Tickflo.Core.Exceptions;
using Tickflo.Web.Services;
using Xunit;

public class WidgetUrlSafetyTests
{
    [Fact]
    public async Task EnsureSafeUrlAsync_WhenNonHttpScheme_ShouldThrowBadRequestException() => await Assert.ThrowsAsync<BadRequestException>(() =>
                                                                                                        WidgetUrlSafety.EnsureSafeUrlAsync("ftp://example.com/file", allowPrivateTargets: false, CancellationToken.None));

    [Theory]
    [InlineData("http://192.168.1.1/")]
    [InlineData("http://10.0.0.1/")]
    [InlineData("http://172.16.0.1/")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://169.254.169.254/")]
    [InlineData("http://100.64.0.1/")]
    [InlineData("http://224.0.0.1/")]
    public async Task EnsureSafeUrlAsync_WhenPrivateOrReservedIp_ShouldThrowBadRequestException(string url) => await Assert.ThrowsAsync<BadRequestException>(() =>
                                                                                                                        WidgetUrlSafety.EnsureSafeUrlAsync(url, allowPrivateTargets: false, CancellationToken.None));

    [Theory]
    [InlineData("http://localhost/")]
    [InlineData("http://foo.local/")]
    [InlineData("http://bar.internal/")]
    public async Task EnsureSafeUrlAsync_WhenInternalHostname_ShouldThrowBadRequestException(string url) => await Assert.ThrowsAsync<BadRequestException>(() =>
                                                                                                                     WidgetUrlSafety.EnsureSafeUrlAsync(url, allowPrivateTargets: false, CancellationToken.None));

    [Fact]
    public async Task EnsureSafeUrlAsync_WhenPublicIpLiteral_ShouldNotThrow() => await WidgetUrlSafety.EnsureSafeUrlAsync("http://8.8.8.8/", allowPrivateTargets: false, CancellationToken.None);

    [Theory]
    [InlineData("http://192.168.1.1/")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://localhost/")]
    public async Task EnsureSafeUrlAsync_WhenPrivateTargetsAllowed_ShouldNotThrow(string url) => await WidgetUrlSafety.EnsureSafeUrlAsync(url, allowPrivateTargets: true, CancellationToken.None);
}
