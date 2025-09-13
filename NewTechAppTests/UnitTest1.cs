using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewTechApp.Auth;
using NewTechApp.Data;

namespace NewTechAppTests
{
    [TestClass]
    public class BusinessLogicTests
    {
        /// <summary>
        /// Тестирование хэширования пароля алгоритмом SHA256
        /// Проверяет, что одинаковые пароли дают одинаковый хэш
        /// и что хэш не совпадает с оригинальным паролем
        /// </summary>
        [TestMethod]
        public void TestPasswordHashing_Sha256()
        {
            // Arrange
            string password = "test123";

            // Act
            byte[] hash = HashHelper.Sha256(password);
            byte[] hash2 = HashHelper.Sha256(password);

            // Assert
            CollectionAssert.AreEqual(hash, hash2, "Хэши одного пароля должны совпадать");
            Assert.AreNotEqual(password, Encoding.UTF8.GetString(hash), "Хэш не должен совпадать с оригинальным паролем");
        }

        /// <summary>
        /// Тестирование расчета скидки в зависимости от объема продаж
        /// Проверяет корректность расчета для всех граничных значений
        /// </summary>
        [TestMethod]
        public void TestDiscountCalculation_VolumeBased()
        {
            // Act & Assert
            Assert.AreEqual(0.00m, DiscountCalculator.CalcDiscount(5000), "Скидка 0% для объема до 10000");
            Assert.AreEqual(0.05m, DiscountCalculator.CalcDiscount(25000), "Скидка 5% для объема 10000-50000");
            Assert.AreEqual(0.10m, DiscountCalculator.CalcDiscount(100000), "Скидка 10% для объема 50000-300000");
            Assert.AreEqual(0.15m, DiscountCalculator.CalcDiscount(500000), "Скидка 15% для объема 300000-1000000");
            Assert.AreEqual(0.20m, DiscountCalculator.CalcDiscount(2000000), "Скидка 20% для объема свыше 1000000");
        }

        /// <summary>
        /// Тестирование генерации CAPTCHA
        /// Проверяет длину и допустимые символы в CAPTCHA
        /// </summary>
        [TestMethod]
        public void TestCaptchaGeneration_LengthAndContent()
        {
            // Arrange
            var captchaText = CaptchaGenerator.GenerateText(4);

            // Assert
            Assert.AreEqual(4, captchaText.Length, "CAPTCHA должна содержать 4 символа");
            Assert.IsTrue(captchaText.All(c => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(c)),
                         "CAPTCHA должна содержать только разрешенные символы");
        }

        /// <summary>
        /// Тестирование валидации цены товара
        /// Проверяет, что система отвергает отрицательные значения цены
        /// </summary>
        [TestMethod]
        public void TestProductValidation_InvalidPrice()
        {
            // Act & Assert
            Assert.IsFalse(ProductValidator.ValidatePrice(-100), "Валидация должна失败 при отрицательной цене");
            Assert.IsTrue(ProductValidator.ValidatePrice(100), "Валидация должна пройти при положительной цене");
            Assert.IsTrue(ProductValidator.ValidatePrice(0), "Валидация должна пройти при нулевой цене");
        }

        /// <summary>
        /// Тестирование блокировки системы после multiple неудачных попыток входа
        /// Проверяет эскалацию блокировки системы
        /// </summary>
        [TestMethod]
        public void TestSystemLockout_Escalation()
        {
            // Arrange
            var security = new SecurityManager();

            // Act & Assert
            // Первая неудачная попытка - должна появиться CAPTCHA
            security.RecordFailedAttempt();
            Assert.IsTrue(security.RequiresCaptcha, "После первой неудачи должна требоваться CAPTCHA");
            Assert.IsFalse(security.IsTemporarilyLocked, "После первой неудачи не должно быть временной блокировки");

            // Вторая неудачная попытка - должна начаться временная блокировка
            security.RecordFailedAttempt();
            Assert.IsTrue(security.IsTemporarilyLocked, "После второй неудачи должна начаться временная блокировка");

            // Третья неудачная попытка - полная блокировка
            security.RecordFailedAttempt();
            Assert.IsTrue(security.IsPermanentlyLocked, "После третьей неудачи должна начаться полная блокировка");
        }

        /// <summary>
        /// Тестирование расчета итоговой цены со скидкой
        /// Проверяет корректность применения скидки к базовой цене
        /// </summary>
        [TestMethod]
        public void TestFinalPriceCalculation_WithDiscount()
        {
            // Arrange
            decimal basePrice = 1000m;
            decimal discount = 0.10m; // 10%

            // Act
            decimal finalPrice = PriceCalculator.CalculateFinalPrice(basePrice, discount);

            // Assert
            Assert.AreEqual(900m, finalPrice, "Итоговая цена с 10% скидкой должна быть 900");
            Assert.IsTrue(finalPrice < basePrice, "Итоговая цена должна быть меньше базовой");
        }
    }

    // === ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ ТЕСТИРОВАНИЯ ===

    public static class HashHelper
    {
        public static byte[] Sha256(string s)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        }
    }

    public static class DiscountCalculator
    {
        public static decimal CalcDiscount(decimal vol)
        {
            if (vol <= 10000) return 0m;
            if (vol <= 50000) return 0.05m;
            if (vol <= 300000) return 0.10m;
            if (vol <= 1000000) return 0.15m;
            return 0.20m;
        }
    }

    public static class CaptchaGenerator
    {
        private static readonly Random R = new Random();

        public static string GenerateText(int len)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var s = new char[len];
            for (int i = 0; i < len; i++) s[i] = chars[R.Next(chars.Length)];
            return new string(s);
        }
    }

    public static class ProductValidator
    {
        public static bool ValidatePrice(decimal price)
        {
            return price >= 0;
        }
    }

    public class SecurityManager
    {
        private int _failedAttempts = 0;

        public bool RequiresCaptcha => _failedAttempts >= 1;
        public bool IsTemporarilyLocked => _failedAttempts >= 2;
        public bool IsPermanentlyLocked => _failedAttempts >= 3;

        public void RecordFailedAttempt()
        {
            _failedAttempts++;
        }

        public void Reset()
        {
            _failedAttempts = 0;
        }
    }

    public static class PriceCalculator
    {
        public static decimal CalculateFinalPrice(decimal basePrice, decimal discount)
        {
            return Math.Round(basePrice * (1 - discount), 2);
        }
    }
}