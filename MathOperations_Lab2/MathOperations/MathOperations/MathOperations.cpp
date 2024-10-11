#include <iostream>
#include <cstdlib>
#include <ctime>
#include <windows.h>
#include <conio.h>

long long gcd(long long a, long long b);
long long mod_pow(long long base, long long exp, long long mod);
long long extended_gcd(long long a, long long b, long long& x, long long& y);
long long mod_inverse(long long a, long long mod);
bool miller_test(long long d, long long n);
long long generate_prime(int bits);
void clear_console();
void display_menu();

int main() {
	// Установка кодировки консоли на CP1251 (для корректного отображения кириллицы)
	SetConsoleOutputCP(1251);
	SetConsoleCP(1251);

	int choice;
	while (true) {
		display_menu();
		std::cout << "Введите номер операции: ";
		std::cin >> choice;
		std::cout << std::endl;

		if (choice == 1) {
			long long base, exp, mod;
			std::cout << "Введите основание: ";
			std::cin >> base;
			std::cout << "Введите степень: ";
			std::cin >> exp;
			std::cout << "Введите модуль: ";
			std::cin >> mod;
			std::cout << base << "^" << exp << " mod " << mod << " = " << mod_pow(base, exp, mod) << std::endl;
		}
		else if (choice == 2) {
			long long a, b;
			std::cout << "Введите первое число: ";
			std::cin >> a;
			std::cout << "Введите второе число: ";
			std::cin >> b;
			std::cout << "gcd(" << a << ", " << b << ") = " << gcd(a, b) << std::endl;
		}
		else if (choice == 3) {
			long long a, mod;
			std::cout << "Введите число: ";
			std::cin >> a;
			std::cout << "Введите модуль: ";
			std::cin >> mod;
			try {
				std::cout << "Обратный элемент " << a << " в кольце по модулю " << mod << " = " << mod_inverse(a, mod) << std::endl;
			}
			catch (const std::invalid_argument& e) {
				std::cerr << e.what() << std::endl;
			}
		}
		else if (choice == 4) {
			int bits;
			std::cout << "Введите количество бит: ";
			std::cin >> bits;
			std::cout << "Сгенерированное простое число: " << generate_prime(bits) << std::endl;
		}
		else if (choice == 5) {
			std::cout << "Выход из программы." << std::endl;
			break;
		}
		else {
			std::cout << "Неверный выбор, попробуйте снова." << std::endl;
		}

		// Ожидание нажатия любой клавиши
		std::cout << std::endl << "Нажмите любую клавишу для продолжения..." << std::endl;
		_getch(); // Ожидает нажатие клавиши

		// Очистка консоли
		clear_console();
	}

	return 0;
}

// Быстрое возведение в степень по модулю
long long mod_pow(long long base, long long exp, long long mod) {
	long long result = 1;
	base = base % mod;

	while (exp > 0) {
		if (exp % 2 == 1) {
			result = (result * base) % mod;
		}
		exp = exp >> 1;
		base = (base * base) % mod;
	}

	return result;
}

// Алгоритм Евклида для нахождения наибольшего общего делителя (НОД)
long long gcd(long long a, long long b) {
	while (b != 0) {
		long long temp = b;
		b = a % b;
		a = temp;
	}
	return a;
}

// Расширенный алгоритм Евклида для нахождения обратного элемента
long long extended_gcd(long long a, long long b, long long& x, long long& y) {
	if (b == 0) {
		x = 1;
		y = 0;
		return a;
	}
	long long x1, y1;
	long long gcd = extended_gcd(b, a % b, x1, y1);
	x = y1;
	y = x1 - (a / b) * y1;
	return gcd;
}

// Нахождение обратного элемента в кольце вычетов по модулю
long long mod_inverse(long long a, long long mod) {
	long long x, y;
	long long g = extended_gcd(a, mod, x, y);
	if (g != 1) {
		throw std::invalid_argument("Обратный элемент не существует");
	}
	return (x % mod + mod) % mod;
}

// Быстрое возведение в степень по модулю для теста Рабина-Миллера
bool miller_test(long long d, long long n) {
	long long a = 2 + rand() % (n - 4);
	long long x = mod_pow(a, d, n);

	if (x == 1 || x == n - 1) {
		return true;
	}

	while (d != n - 1) {
		x = (x * x) % n;
		d *= 2;

		if (x == 1) return false;
		if (x == n - 1) return true;
	}

	return false;
}

// Тест Рабина-Миллера для проверки простоты числа
bool is_prime(long long n, int k) {
	if (n <= 1 || n == 4) return false;
	if (n <= 3) return true;

	long long d = n - 1;
	while (d % 2 == 0) {
		d /= 2;
	}

	for (int i = 0; i < k; i++) {
		if (!miller_test(d, n)) {
			return false;
		}
	}
	return true;
}

// Генерация случайного простого числа
long long generate_prime(int bits) {
	long long candidate;
	int k = 4; // Количество раундов теста Рабина-Миллера
	srand(time(nullptr));

	do {
		candidate = rand() % ((1LL << bits) - 1) + (1LL << (bits - 1));
	} while (!is_prime(candidate, k));

	return candidate;
}

void display_menu() {
	std::cout << "Выберите операцию:\n";
	std::cout << "1. Возведение в степень в кольце вычетов\n";
	std::cout << "2. Нахождение НОД (Алгоритм Евклида)\n";
	std::cout << "3. Нахождение обратного элемента в кольце вычетов\n";
	std::cout << "4. Генерация большого простого числа (Тест Рабина-Миллера)\n";
	std::cout << "5. Выход\n";
	std::cout << std::endl;
}

void clear_console() {
	system("cls");
}