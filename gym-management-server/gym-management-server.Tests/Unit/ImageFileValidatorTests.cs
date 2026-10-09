using gym_management_server.Infrastructure.Images;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ImageFileValidatorTests
    {
        [Fact]
        public void Accepts_a_real_png_header()
        {
            byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
            Assert.True(ImageFileValidator.LooksLikeImage(png, ".png"));
        }

        [Fact]
        public void Accepts_a_real_jpeg_header()
        {
            byte[] jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
            Assert.True(ImageFileValidator.LooksLikeImage(jpeg, ".jpg"));
            Assert.True(ImageFileValidator.LooksLikeImage(jpeg, ".jpeg"));
        }

        [Fact]
        public void Accepts_a_real_webp_header()
        {
            byte[] webp = { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
            Assert.True(ImageFileValidator.LooksLikeImage(webp, ".webp"));
        }

        [Fact]
        public void Rejects_a_png_extension_on_non_image_bytes()
        {
            byte[] fakeContent = System.Text.Encoding.ASCII.GetBytes("<?php system($_GET['c']); ?>");
            Assert.False(ImageFileValidator.LooksLikeImage(fakeContent, ".png"));
        }

        [Fact]
        public void Rejects_a_jpeg_header_declared_as_png()
        {
            byte[] jpeg = { 0xFF, 0xD8, 0xFF, 0xE0 };
            Assert.False(ImageFileValidator.LooksLikeImage(jpeg, ".png"));
        }

        [Fact]
        public void Rejects_a_truncated_header()
        {
            byte[] tooShort = { 0x89, 0x50 };
            Assert.False(ImageFileValidator.LooksLikeImage(tooShort, ".png"));
        }
    }
}
