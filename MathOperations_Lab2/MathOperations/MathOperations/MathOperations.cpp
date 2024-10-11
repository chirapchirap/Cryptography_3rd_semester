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
	// ��������� ��������� ������� �� CP1251 (��� ����������� ����������� ���������)
	SetConsoleOutputCP(1251);
	SetConsoleCP(1251);

	int choice;
	while (true) {
		display_menu();
		std::cout << "������� ����� ��������: ";
		std::cin >> choice;
		std::cout << std::endl;

		if (choice == 1) {
			long long base, exp, mod;
			std::cout << "������� ���������: ";
			std::cin >> base;
			std::cout << "������� �������: ";
			std::cin >> exp;
			std::cout << "������� ������: ";
			std::cin >> mod;
			std::cout << base << "^" << exp << " mod " << mod << " = " << mod_pow(base, exp, mod) << std::endl;
		}
		else if (choice == 2) {
			long long a, b;
			std::cout << "������� ������ �����: ";
			std::cin >> a;
			std::cout << "������� ������ �����: ";
			std::cin >> b;
			std::cout << "gcd(" << a << ", " << b << ") = " << gcd(a, b) << std::endl;
		}
		else if (choice == 3) {
			long long a, mod;
			std::cout << "������� �����: ";
			std::cin >> a;
			std::cout << "������� ������: ";
			std::cin >> mod;
			try {
				std::cout << "�������� ������� " << a << " � ������ �� ������ " << mod << " = " << mod_inverse(a, mod) << std::endl;
			}
			catch (const std::invalid_argument& e) {
				std::cerr << e.what() << std::endl;
			}
		}
		else if (choice == 4) {
			int bits;
			std::cout << "������� ���������� ���: ";
			std::cin >> bits;
			std::cout << "��������������� ������� �����: " << generate_prime(bits) << std::endl;
		}
		else if (choice == 5) {
			std::cout << "����� �� ���������." << std::endl;
			break;
		}
		else {
			std::cout << "�������� �����, ���������� �����." << std::endl;
		}

		// �������� ������� ����� �������
		std::cout << std::endl << "������� ����� ������� ��� �����������..." << std::endl;
		_getch(); // ������� ������� �������

		// ������� �������
		clear_console();
	}

	return 0;
}

// ������� ���������� � ������� �� ������
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

// �������� ������� ��� ���������� ����������� ������ �������� (���)
long long gcd(long long a, long long b) {
	while (b != 0) {
		long long temp = b;
		b = a % b;
		a = temp;
	}
	return a;
}

// ����������� �������� ������� ��� ���������� ��������� ��������
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

// ���������� ��������� �������� � ������ ������� �� ������
long long mod_inverse(long long a, long long mod) {
	long long x, y;
	long long g = extended_gcd(a, mod, x, y);
	if (g != 1) {
		throw std::invalid_argument("�������� ������� �� ����������");
	}
	return (x % mod + mod) % mod;
}

// ������� ���������� � ������� �� ������ ��� ����� ������-�������
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

// ���� ������-������� ��� �������� �������� �����
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

// ��������� ���������� �������� �����
long long generate_prime(int bits) {
	long long candidate;
	int k = 4; // ���������� ������� ����� ������-�������
	srand(time(nullptr));

	do {
		candidate = rand() % ((1LL << bits) - 1) + (1LL << (bits - 1));
	} while (!is_prime(candidate, k));

	return candidate;
}

void display_menu() {
	std::cout << "�������� ��������:\n";
	std::cout << "1. ���������� � ������� � ������ �������\n";
	std::cout << "2. ���������� ��� (�������� �������)\n";
	std::cout << "3. ���������� ��������� �������� � ������ �������\n";
	std::cout << "4. ��������� �������� �������� ����� (���� ������-�������)\n";
	std::cout << "5. �����\n";
	std::cout << std::endl;
}

void clear_console() {
	system("cls");
}