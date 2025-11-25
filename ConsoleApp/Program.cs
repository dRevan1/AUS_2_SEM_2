using SEM_2_CORE;
using SEM_2_CORE.Testers;

Person person = new Person("John", "Wick", 15, 6, 1990, "1");
HeapFileTester tester = new HeapFileTester();
HeapFile<Person> heapFile = new HeapFile<Person>("people.bin", 200, person);
tester.InsertData(heapFile, 3);
