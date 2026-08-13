using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace SettingsAll.Tests;

public class DllScannerTests
{
    private HashSet<string> Scan(byte[] buffer)
    {
        return DllScanner.ExtractFromBytes(buffer, CancellationToken.None);
    }

    [Fact]
    public void Test1_AsciiBoundaryDetection()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:display\0");
        var res = Scan(buffer);
        Assert.Contains("ms-settings:display", res);
    }

    [Fact]
    public void Test2_Utf16BoundaryDetection()
    {
        byte[] bytes = Encoding.Unicode.GetBytes("\0ms-settings:sound\0");
        var res = Scan(bytes);
        Assert.Contains("ms-settings:sound", res);
    }

    [Fact]
    public void Test3_FragmentRejection()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("abcms-settings:foo\0");
        var res = Scan(buffer);
        Assert.Empty(res);
    }

    [Fact]
    public void Test4_TemplateFiltering()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:page-%s-detail\0");
        var res = Scan(buffer);
        Assert.Empty(res);
    }

    [Fact]
    public void Test5_LengthBounds()
    {
        byte[] buffer1 = Encoding.ASCII.GetBytes("\0ms-settings:\0");
        Assert.Empty(Scan(buffer1));

        string longUri = "ms-settings:" + new string('a', 200);
        byte[] buffer2 = Encoding.ASCII.GetBytes("\0" + longUri + "\0");
        Assert.Empty(Scan(buffer2));
    }

    [Fact]
    public void Test6_QueryStringDedup_BareWins()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:foo\0\0ms-settings:foo?bar=1\0");
        var res = Scan(buffer);
        Assert.Single(res);
        Assert.Contains("ms-settings:foo", res);
    }

    [Fact]
    public void Test7_QueryStringDedup_NoBareVariant()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:foo?baz=2&x=3\0\0ms-settings:foo?bar=1\0");
        var res = Scan(buffer);
        Assert.Single(res);
        Assert.Contains("ms-settings:foo?bar=1", res);
    }

    [Fact]
    public void Test8_SpaceImplicitTerminator()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:page <bad>\0");
        var res = Scan(buffer);
        Assert.Single(res);
        Assert.Contains("ms-settings:page", res);
    }

    [Fact]
    public void Test9_ValidQueryChars()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:page?key=val&x=1\0");
        var res = Scan(buffer);
        Assert.Contains("ms-settings:page?key=val&x=1", res);
    }

    [Fact]
    public void Test10_InvalidQueryChars()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:page?key=<script>\0");
        var res = Scan(buffer);
        Assert.Empty(res);
    }

    [Fact]
    public void Test11_MixedEncodingDedup()
    {
        List<byte> bytes = new();
        bytes.AddRange(Encoding.ASCII.GetBytes("\0ms-settings:foo?bar=1\0"));
        bytes.AddRange(Encoding.Unicode.GetBytes("\0ms-settings:foo\0"));
        
        var res = Scan(bytes.ToArray());
        Assert.Single(res);
        Assert.Contains("ms-settings:foo", res);
    }

    [Fact]
    public void Test12_LengthPrefixedBoundary()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\x0Ems-settings:foo\0");
        var res = Scan(buffer);
        Assert.Contains("ms-settings:foo", res);
    }

    [Fact]
    public void Test13_StartOfBufferBoundary()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("ms-settings:foo\0");
        var res = Scan(buffer);
        Assert.Contains("ms-settings:foo", res);
    }

    [Fact]
    public void Test14_UnterminatedTrailingString()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("\0ms-settings:foo");
        var res = Scan(buffer);
        Assert.Empty(res);
    }
}
