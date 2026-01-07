package com.example.medicinecontrolsystem.data

data class Caregiver(val caregiverId: Int, val name: String, val username: String)
val initialCaregivers = listOf(
    Caregiver(1, "王护工", "caregiver01")
)