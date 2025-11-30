using SEM_2_CORE;
using SEM_2_CORE.Files;
using SEM_2_CORE.Testers;

LinearHashFileTester linTester = new LinearHashFileTester();
HeapFileTester heapTester = new HeapFileTester();
Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
LinearHashFile<Person> linHashFile = new LinearHashFile<Person>(4, "test.bin", "overflow.bin", 500, 220, dataInstance);

linTester.InsertTest(linHashFile, 100_000);