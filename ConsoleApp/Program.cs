using SEM_2_CORE;
using SEM_2_CORE.Files;
using SEM_2_CORE.Testers;

LinearHashFileTester linTester = new LinearHashFileTester();
HeapFileTester heapTester = new HeapFileTester();
Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
int primaryBlockSize = 500, overflowBlockSize = 220, mod = 4;
LinearHashFile<Person> linHashFile = new LinearHashFile<Person>(mod, "test.bin", "overflow.bin", primaryBlockSize, overflowBlockSize, dataInstance);

linTester.LinearHashTest(linHashFile, 1_000_000);