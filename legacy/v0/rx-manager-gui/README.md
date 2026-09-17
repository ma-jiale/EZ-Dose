package cmd

nuitka version >= 2.7.9

```
python -m nuitka --standalone --enable-plugin=pyside6 --follow-imports --include-data-files=patients.csv=patients.csv --include-data-files=config.json=config.json --include-data-files=local_prescriptions_data.csv=local_prescriptions_data.csv --output-dir=dist rx_manager_gui.py
```

