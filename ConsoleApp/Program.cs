using SEM_2_CORE;

Person person = new Person("John", "Wick", 15, 6, 1990, "1");
HeapFileTester tester = new HeapFileTester();

tester.DeleteTest("test.bin", 834, 1000, 3000);