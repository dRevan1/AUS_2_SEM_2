using SEM_2_CORE;

Person person = new Person("John", "Wick", 15, 6, 1990, "1");
Person person1 = new Person("Molag", "Bal", 22, 11, 1985, "2");
Person person2 = new Person("Gordon", "Freeman", 9, 4, 1995, "3");

HeapFile<Person> heapFile = new HeapFile<Person>("test.bin", 174, person);
heapFile.Insert(person);

heapFile.Insert(person1);

heapFile.Insert(person2);

heapFile.Delete(1, person2);
heapFile.Delete(0, person);
heapFile.Delete(0, person1);

heapFile.Insert(person2);
heapFile.Delete(0, person2);

heapFile.Get(0, person);
