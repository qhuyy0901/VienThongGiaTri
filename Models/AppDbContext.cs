using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace AspNetMvcApp.Models;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<ProductSpecification> ProductSpecifications { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Category-Product One-to-Many Relationship
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Price Precision to fix EF Core Warning
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Camera Giám Sát" },
            new Category { Id = 2, Name = "Thiết Bị Switch" },
            new Category { Id = 3, Name = "Thiết Bị Mạng và Phụ Kiện" }
        );

        // Seed 10 Sample Products using the images in wwwroot/imager
        modelBuilder.Entity<Product>().HasData(
            new Product 
            { 
                Id = 1, 
                Name = "Camera Hikvision DS-2CV2021G2-IDW", 
                CategoryId = 1, 
                Price = 42.00m, 
                Rating = 4.8, 
                Description = "Camera IP Wifi ngoài trời 2MP chất lượng cao, tích hợp mic và loa hỗ trợ đàm thoại 2 chiều, chuẩn chống nước IP66 bền bỉ.", 
                ImagePath = "camera-ip-wifi-2mp-hikvision-ds-2cv2021g2-idw-1.jpg", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 2, 
                Name = "Camera Imou IPC-A52P 360°", 
                CategoryId = 1, 
                Price = 30.00m, 
                Rating = 4.7, 
                Description = "Camera Wifi quay quét thông minh trong nhà với độ phân giải 5MP siêu nét, phát hiện chuyển động bằng AI và bám theo đối tượng.", 
                ImagePath = "camera-wifi-360-do-imou-ipc-a52p.jpg", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 3, 
                Name = "Camera Imou Cruiser Z 3K", 
                CategoryId = 1, 
                Price = 78.00m, 
                Rating = 4.9, 
                Description = "Camera Wifi ngoài trời quay quét PTZ độ phân giải 3K, zoom quang học tích hợp đèn spotlight cảnh báo ban đêm có màu sinh động.", 
                ImagePath = "camera-wifi-imou-ipc-s7dp-5m0wez-cruiser-z-3k.jpg", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 4, 
                Name = "Switch Ruijie Reyee RG-ES205GC", 
                CategoryId = 2, 
                Price = 28.00m, 
                Rating = 4.6, 
                Description = "Bộ chuyển mạch Smart Managed 5 cổng Gigabit chuyên dụng cho hệ thống camera, cấu hình quản lý dễ dàng qua Ruijie Cloud.", 
                ImagePath = "switch_24port.png", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 5, 
                Name = "Switch PoE Ruijie RG-ES209GC-P", 
                CategoryId = 2, 
                Price = 88.00m, 
                Rating = 4.8, 
                Description = "Switch Smart Managed 9 cổng Gigabit với 8 cổng hỗ trợ nguồn PoE tổng công suất 120W, lý tưởng để cấp nguồn cho hệ thống Camera IP.", 
                ImagePath = "switch_24port.png", 
                StockStatus = "LowStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 6, 
                Name = "Switch Cisco Catalyst C9200L", 
                CategoryId = 2, 
                Price = 980.00m, 
                Rating = 4.9, 
                Description = "Bộ chuyển mạch cao cấp Cisco 24 cổng Gigabit, 4 cổng uplink 10G SFP+, bảo mật doanh nghiệp vượt trội và hiệu năng chuyển mạch băng thông rộng.", 
                ImagePath = "switch_24port.png", 
                StockStatus = "InStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 7, 
                Name = "Bộ Phát Wi-Fi 6 Ruijie RG-RAP2260(G)", 
                CategoryId = 3, 
                Price = 105.00m, 
                Rating = 4.7, 
                Description = "Router Access Point gắn trần chuẩn Wi-Fi 6 tốc độ lên tới 1775Mbps, chịu tải cực mạnh lên tới 120 user đồng thời cho văn phòng.", 
                ImagePath = "router_wifi6.png", 
                StockStatus = "InStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 8, 
                Name = "Router MikroTik hEX gr3", 
                CategoryId = 3, 
                Price = 64.00m, 
                Rating = 4.8, 
                Description = "Bộ định tuyến cân bằng tải mạng chuyên nghiệp, tích hợp 5 cổng mạng Gigabit, cấu hình RouterOS đa tính năng định tuyến mạnh mẽ.", 
                ImagePath = "router_wifi6.png", 
                StockStatus = "LowStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 9, 
                Name = "Module Quang SFP Ruijie 1G", 
                CategoryId = 3, 
                Price = 22.00m, 
                Rating = 4.5, 
                Description = "Module SFP truyền dẫn quang học khoảng cách xa lên đến 10km, kết nối chuẩn LC Single-Mode hiệu suất ổn định vượt trội.", 
                ImagePath = "sfp_transceiver.png", 
                StockStatus = "InStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 10, 
                Name = "Cáp Mạng CommScope Cat6 UTP 305m", 
                CategoryId = 3, 
                Price = 118.00m, 
                Rating = 4.9, 
                Description = "Cáp mạng chống nhiễu Cat6 UTP chính hãng CommScope, cuộn dài 305m lõi đồng nguyên chất cho tốc độ truyền dẫn Gigabit ổn định.", 
                ImagePath = "network_cable.png", 
                StockStatus = "InStock", 
                IsFeatured = false 
            }
        );

        // Configure Product-ProductImage One-to-Many Relationship
        modelBuilder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.ProductImages)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed Product Images for Details Gallery (3 images per product)
        modelBuilder.Entity<ProductImage>().HasData(
            // Product 1: Hikvision
            new ProductImage { Id = 1, ProductId = 1, ImagePath = "camera-ip-wifi-2mp-hikvision-ds-2cv2021g2-idw-1.jpg", DisplayOrder = 0 },
            new ProductImage { Id = 2, ProductId = 1, ImagePath = "camera-ip-wifi-2mp-hikvision-ds-2cv2021g2-idw-2.jpg", DisplayOrder = 1 },
            new ProductImage { Id = 3, ProductId = 1, ImagePath = "camera-ip-wifi-2mp-hikvision-ds-2cv2021g2-idw-3.jpg", DisplayOrder = 2 },

            // Product 2: Imou 360
            new ProductImage { Id = 4, ProductId = 2, ImagePath = "camera-wifi-360-do-imou-ipc-a52p.jpg", DisplayOrder = 0 },
            new ProductImage { Id = 5, ProductId = 2, ImagePath = "camera-wifi-360-do-imou-ipc-a52p-1.jpg", DisplayOrder = 1 },
            new ProductImage { Id = 6, ProductId = 2, ImagePath = "camera-wifi-360-do-imou-ipc-a52p-2.jpg", DisplayOrder = 2 },

            // Product 3: Imou Cruiser Z
            new ProductImage { Id = 7, ProductId = 3, ImagePath = "camera-wifi-imou-ipc-s7dp-5m0wez-cruiser-z-3k.jpg", DisplayOrder = 0 },
            new ProductImage { Id = 8, ProductId = 3, ImagePath = "camera-wifi-imou-ipc-s7dp-5m0wez-cruiser-z-3k-2.jpg", DisplayOrder = 1 },
            new ProductImage { Id = 9, ProductId = 3, ImagePath = "camera-wifi-imou-ipc-s7dp-5m0wez-cruiser-z-3k-3.jpg", DisplayOrder = 2 },

            // Product 4: Ruijie Switch
            new ProductImage { Id = 10, ProductId = 4, ImagePath = "switch_24port.png", DisplayOrder = 0 },
            new ProductImage { Id = 11, ProductId = 4, ImagePath = "switch_24port.png", DisplayOrder = 1 },
            new ProductImage { Id = 12, ProductId = 4, ImagePath = "switch_24port.png", DisplayOrder = 2 },

            // Product 5: Ruijie PoE Switch
            new ProductImage { Id = 13, ProductId = 5, ImagePath = "switch_24port.png", DisplayOrder = 0 },
            new ProductImage { Id = 14, ProductId = 5, ImagePath = "switch_24port.png", DisplayOrder = 1 },
            new ProductImage { Id = 15, ProductId = 5, ImagePath = "switch_24port.png", DisplayOrder = 2 },

            // Product 6: Cisco Switch
            new ProductImage { Id = 16, ProductId = 6, ImagePath = "switch_24port.png", DisplayOrder = 0 },
            new ProductImage { Id = 17, ProductId = 6, ImagePath = "switch_24port.png", DisplayOrder = 1 },
            new ProductImage { Id = 18, ProductId = 6, ImagePath = "switch_24port.png", DisplayOrder = 2 },

            // Product 7: AP Ruijie
            new ProductImage { Id = 19, ProductId = 7, ImagePath = "router_wifi6.png", DisplayOrder = 0 },
            new ProductImage { Id = 20, ProductId = 7, ImagePath = "router_wifi6.png", DisplayOrder = 1 },
            new ProductImage { Id = 21, ProductId = 7, ImagePath = "router_wifi6.png", DisplayOrder = 2 },

            // Product 8: Router MikroTik
            new ProductImage { Id = 22, ProductId = 8, ImagePath = "router_wifi6.png", DisplayOrder = 0 },
            new ProductImage { Id = 23, ProductId = 8, ImagePath = "router_wifi6.png", DisplayOrder = 1 },
            new ProductImage { Id = 24, ProductId = 8, ImagePath = "router_wifi6.png", DisplayOrder = 2 },

            // Product 9: Module SFP
            new ProductImage { Id = 25, ProductId = 9, ImagePath = "sfp_transceiver.png", DisplayOrder = 0 },
            new ProductImage { Id = 26, ProductId = 9, ImagePath = "sfp_transceiver.png", DisplayOrder = 1 },
            new ProductImage { Id = 27, ProductId = 9, ImagePath = "sfp_transceiver.png", DisplayOrder = 2 },

            // Product 10: Cab CommScope
            new ProductImage { Id = 28, ProductId = 10, ImagePath = "network_cable.png", DisplayOrder = 0 },
            new ProductImage { Id = 29, ProductId = 10, ImagePath = "network_cable.png", DisplayOrder = 1 },
            new ProductImage { Id = 30, ProductId = 10, ImagePath = "network_cable.png", DisplayOrder = 2 }
        );

        // Configure Product-ProductSpecification One-to-Many Relationship
        modelBuilder.Entity<ProductSpecification>()
            .HasOne(ps => ps.Product)
            .WithMany(p => p.ProductSpecifications)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed Product Specifications for Database-Driven Specs Table
        modelBuilder.Entity<ProductSpecification>().HasData(
            // Product 1: Camera Hikvision DS-2CV2021G2-IDW
            new ProductSpecification { Id = 1, ProductId = 1, Name = "Thương hiệu", Value = "Hikvision", DisplayOrder = 0 },
            new ProductSpecification { Id = 2, ProductId = 1, Name = "Độ phân giải", Value = "2.0 Megapixel (1920x1080) Full HD", DisplayOrder = 1 },
            new ProductSpecification { Id = 3, ProductId = 1, Name = "Góc quan sát", Value = "Góc rộng 112 độ ngang, 136 độ chéo", DisplayOrder = 2 },
            new ProductSpecification { Id = 4, ProductId = 1, Name = "Tầm nhìn xa hồng ngoại", Value = "Hồng ngoại thông minh EXIR 2.0 tầm xa 30 mét", DisplayOrder = 3 },
            new ProductSpecification { Id = 5, ProductId = 1, Name = "Chuẩn chống nước", Value = "IP66 chịu đựng mưa bão ngoài trời xuất sắc", DisplayOrder = 4 },
            new ProductSpecification { Id = 6, ProductId = 1, Name = "Hỗ trợ đàm thoại", Value = "Tích hợp micrô và loa lọc âm hỗ trợ đàm thoại 2 chiều", DisplayOrder = 5 },

            // Product 2: Camera Imou IPC-A52P 360°
            new ProductSpecification { Id = 7, ProductId = 2, Name = "Thương hiệu", Value = "Imou", DisplayOrder = 0 },
            new ProductSpecification { Id = 8, ProductId = 2, Name = "Độ phân giải", Value = "5.0 Megapixel (3K) siêu sắc nét", DisplayOrder = 1 },
            new ProductSpecification { Id = 9, ProductId = 2, Name = "Tính năng quay quét", Value = "Xoay ngang 355 độ, xoay dọc 90 độ phủ rộng toàn cảnh", DisplayOrder = 2 },
            new ProductSpecification { Id = 10, ProductId = 2, Name = "Tính năng AI thông minh", Value = "Phát hiện con người, phát hiện thú cưng, phát hiện âm thanh lạ", DisplayOrder = 3 },
            new ProductSpecification { Id = 11, ProductId = 2, Name = "Chế độ riêng tư", Value = "Nút nhấn che giấu ống kính vật lý một chạm nhanh chóng", DisplayOrder = 4 },
            new ProductSpecification { Id = 12, ProductId = 2, Name = "Kết nối không dây", Value = "Wi-Fi chuẩn 2.4GHz truyền tải ổn định", DisplayOrder = 5 },

            // Product 3: Camera Imou Cruiser Z 3K
            new ProductSpecification { Id = 13, ProductId = 3, Name = "Thương hiệu", Value = "Imou", DisplayOrder = 0 },
            new ProductSpecification { Id = 14, ProductId = 3, Name = "Độ phân giải", Value = "5.0 Megapixel (3K) siêu nét", DisplayOrder = 1 },
            new ProductSpecification { Id = 15, ProductId = 3, Name = "Hỗ trợ Zoom quang", Value = "Zoom quang học 12x phóng to chi tiết không vỡ ảnh", DisplayOrder = 2 },
            new ProductSpecification { Id = 16, ProductId = 3, Name = "Đèn cảnh báo ban đêm", Value = "Spotlight tích hợp cảnh báo ban đêm có màu 30 mét", DisplayOrder = 3 },
            new ProductSpecification { Id = 17, ProductId = 3, Name = "Chuẩn chống nước vỏ máy", Value = "IP66 hoạt động ổn định bất kể thời tiết nắng mưa", DisplayOrder = 4 },
            new ProductSpecification { Id = 18, ProductId = 3, Name = "Báo động răn đe", Value = "Còi hú báo động kết hợp chớp đèn cảnh báo xâm nhập", DisplayOrder = 5 },

            // Product 4: Switch Ruijie Reyee RG-ES205GC
            new ProductSpecification { Id = 19, ProductId = 4, Name = "Thương hiệu", Value = "Ruijie Reyee", DisplayOrder = 0 },
            new ProductSpecification { Id = 20, ProductId = 4, Name = "Số lượng cổng mạng", Value = "5 Cổng 10/100/1000 Mbps Gigabit Ethernet", DisplayOrder = 1 },
            new ProductSpecification { Id = 21, ProductId = 4, Name = "Băng thông chuyển mạch", Value = "10 Gbps hiệu năng cao không nghẽn", DisplayOrder = 2 },
            new ProductSpecification { Id = 22, ProductId = 4, Name = "Khả năng quản lý Cloud", Value = "Quản trị miễn phí trọn đời qua app Ruijie Cloud từ xa", DisplayOrder = 3 },
            new ProductSpecification { Id = 23, ProductId = 4, Name = "Chất liệu vỏ máy", Value = "Vỏ nhựa cao cấp chịu nhiệt, chống va đập tốt", DisplayOrder = 4 },

            // Product 5: Switch PoE Ruijie RG-ES209GC-P
            new ProductSpecification { Id = 24, ProductId = 5, Name = "Thương hiệu", Value = "Ruijie Reyee", DisplayOrder = 0 },
            new ProductSpecification { Id = 25, ProductId = 5, Name = "Số cổng mạng", Value = "9 Cổng Gigabit (8 cổng PoE và 1 cổng Uplink)", DisplayOrder = 1 },
            new ProductSpecification { Id = 26, ProductId = 5, Name = "Tổng công suất nguồn PoE", Value = "Cấp nguồn PoE tối đa 120W cho camera IP tiện lợi", DisplayOrder = 2 },
            new ProductSpecification { Id = 27, ProductId = 5, Name = "Quản lý cáp mạng", Value = "Tính năng xem trạng thái cáp lỗi trực tiếp trên App Cloud", DisplayOrder = 3 },
            new ProductSpecification { Id = 28, ProductId = 5, Name = "Chất liệu vỏ thiết bị", Value = "Vỏ thép sơn tĩnh điện tản nhiệt tự nhiên cực tốt", DisplayOrder = 4 },

            // Product 6: Switch Cisco Catalyst C9200L
            new ProductSpecification { Id = 29, ProductId = 6, Name = "Thương hiệu", Value = "Cisco Catalyst", DisplayOrder = 0 },
            new ProductSpecification { Id = 30, ProductId = 6, Name = "Số lượng cổng Ethernet", Value = "24 Cổng 10/100/1000 Mbps RJ45 cao cấp", DisplayOrder = 1 },
            new ProductSpecification { Id = 31, ProductId = 6, Name = "Cổng Uplink quang", Value = "4 Cổng SFP+ tốc độ 10Gbps truyền tải cực mạnh", DisplayOrder = 2 },
            new ProductSpecification { Id = 32, ProductId = 6, Name = "Băng thông Backplane", Value = "128 Gbps hiệu năng Enterprise siêu khủng", DisplayOrder = 3 },
            new ProductSpecification { Id = 33, ProductId = 6, Name = "Tính năng bảo mật", Value = "Cisco TrustSec, MACsec-128 mã hóa đầu cuối an toàn", DisplayOrder = 4 },

            // Product 7: AP Ruijie RG-RAP2260(G)
            new ProductSpecification { Id = 34, ProductId = 7, Name = "Thương hiệu", Value = "Ruijie Networks", DisplayOrder = 0 },
            new ProductSpecification { Id = 35, ProductId = 7, Name = "Chuẩn Wi-Fi hỗ trợ", Value = "Wi-Fi 6 (802.11ax) thế hệ mới siêu tốc", DisplayOrder = 1 },
            new ProductSpecification { Id = 36, ProductId = 7, Name = "Tốc độ không dây", Value = "Lên tới 1775 Mbps (2.4GHz: 574Mbps, 5GHz: 1201Mbps)", DisplayOrder = 2 },
            new ProductSpecification { Id = 37, ProductId = 7, Name = "Số lượng kết nối đồng thời", Value = "Hỗ trợ chịu tải lên tới 120 người dùng ổn định", DisplayOrder = 3 },
            new ProductSpecification { Id = 38, ProductId = 7, Name = "Nguồn điện cung cấp", Value = "Hỗ trợ nguồn chuẩn PoE 802.3af hoặc nguồn DC 12V/1.5A", DisplayOrder = 4 },

            // Product 8: Router MikroTik hEX gr3
            new ProductSpecification { Id = 39, ProductId = 8, Name = "Thương hiệu", Value = "MikroTik (Latvia)", DisplayOrder = 0 },
            new ProductSpecification { Id = 40, ProductId = 8, Name = "Bộ vi xử lý CPU", Value = "MT7621A 2 nhân, 4 luồng xung nhịp 880 MHz mạnh mẽ", DisplayOrder = 1 },
            new ProductSpecification { Id = 41, ProductId = 8, Name = "Số cổng mạng kết nối", Value = "5 Cổng Gigabit Ethernet 10/100/1000 Mbps", DisplayOrder = 2 },
            new ProductSpecification { Id = 42, ProductId = 8, Name = "Bộ nhớ trong RAM", Value = "256 MB RAM hỗ trợ xử lý luồng dữ liệu lớn", DisplayOrder = 3 },
            new ProductSpecification { Id = 43, ProductId = 8, Name = "Hệ điều hành tích hợp", Value = "MikroTik RouterOS License level 4 chuyên nghiệp", DisplayOrder = 4 },

            // Product 9: Module Quang SFP Ruijie 1G
            new ProductSpecification { Id = 44, ProductId = 9, Name = "Thương hiệu", Value = "Ruijie Networks", DisplayOrder = 0 },
            new ProductSpecification { Id = 45, ProductId = 9, Name = "Kiểu kết nối", Value = "SFP mini-GBIC tương thích mọi thiết bị chuyên dụng", DisplayOrder = 1 },
            new ProductSpecification { Id = 46, ProductId = 9, Name = "Khoảng cách truyền tín hiệu", Value = "Truyền xa tối đa 10km qua cáp quang Single-Mode", DisplayOrder = 2 },
            new ProductSpecification { Id = 47, ProductId = 9, Name = "Kiểu cổng cắm cáp", Value = "Cổng quang chuẩn Duplex LC chất lượng cao", DisplayOrder = 3 },
            new ProductSpecification { Id = 48, ProductId = 9, Name = "Tốc độ truyền quang", Value = "1.25 Gbps truyền dữ liệu quang học không trễ", DisplayOrder = 4 },

            // Product 10: Cáp Mạng CommScope Cat6 UTP 305m
            new ProductSpecification { Id = 49, ProductId = 10, Name = "Thương hiệu", Value = "CommScope AMP chính hãng", DisplayOrder = 0 },
            new ProductSpecification { Id = 50, ProductId = 10, Name = "Chuẩn cáp mạng", Value = "Cat6 UTP chống nhiễu tốc độ cao", DisplayOrder = 1 },
            new ProductSpecification { Id = 51, ProductId = 10, Name = "Chiều dài cuộn", Value = "305 mét đóng gói trong hộp kéo trơn tru", DisplayOrder = 2 },
            new ProductSpecification { Id = 52, ProductId = 10, Name = "Chất liệu lõi dẫn", Value = "100% Đồng nguyên chất, kích cỡ lõi 23 AWG tiêu chuẩn", DisplayOrder = 3 },
            new ProductSpecification { Id = 53, ProductId = 10, Name = "Tốc độ hỗ trợ truyền dẫn", Value = "Lên tới 10 Gigabit Ethernet ổn định, băng thông 250 MHz", DisplayOrder = 4 }
        );
    }
}
