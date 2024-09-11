using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class birthdayCalc : MonoBehaviour
{
    const int daysInYear = 366;
    public int nPeople = 5000;

    // Start is called before the first frame update
    void Start()
    {
        int experimentsWithUnique = 0;
        for(int i = 0; i < 10000; i++) {
            int n = func(nPeople);

            if (n >= 1)
                experimentsWithUnique++;

        }

        Debug.Log((float) experimentsWithUnique / 10000);

    }

    int func(int numbOfPeople) {


        int[] yearDays = new int[daysInYear];

        for (int i = 0; i < daysInYear; i++) {
            yearDays[i] = 0;
        }

        for (int i = 0; i < numbOfPeople; i++) {

            int birthday = Random.Range(0, daysInYear);
            yearDays[birthday]++;
        }

        int uniqueBirthdays = 0;

        for (int i = 0; i < daysInYear; i++) {
            if (yearDays[i] == 1)
                uniqueBirthdays++;
        }

        return uniqueBirthdays;
    }

}
