package com.example.medicinecontrolsystem.data

import androidx.annotation.DrawableRes
import androidx.annotation.StringRes
import com.example.medicinecontrolsystem.R

data class data_Patient(
    val patientId:Int,
    val auntieId: Int,
    @DrawableRes val imageResourceId:String,
    val patientName:String,
    val patientBedNumber:String,
    val patientBarcode:String,
//    val reminderTime:String,
//    val reminderDate: String
)

//val patients = listOf(
//    data_Patient(1, 1, R.drawable.patient1,R.string.patient_1_name,R.string.patient_1_bednumber,"615"),
//    data_Patient(2, 1, R.drawable.patient2,R.string.patient_2_name,R.string.patient_2_bednumber,"332"),
//    data_Patient(3, 1, R.drawable.patient3,R.string.patient_3_name,R.string.patient_3_bednumber,"231"),
//    data_Patient(4, 1, R.drawable.patient1,R.string.patient_4_name,R.string.patient_4_bednumber,"213"),
//    data_Patient(5, 1, R.drawable.patient2,R.string.patient_5_name,R.string.patient_5_bednumber,"333"),
//    data_Patient(6, 2, R.drawable.patient3,R.string.patient_6_name,R.string.patient_6_bednumber,"125"),
//    data_Patient(7, 2, R.drawable.patient1,R.string.patient_7_name,R.string.patient_7_bednumber,"127"),
//    data_Patient(8, 2, R.drawable.patient2,R.string.patient_8_name,R.string.patient_8_bednumber,"215"),
//    data_Patient(9, 2, R.drawable.patient3,R.string.patient_9_name,R.string.patient_9_bednumber,"337"),
//)