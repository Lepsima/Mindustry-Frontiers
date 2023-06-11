using System;

[System.Serializable]
public class SerializableType {
	private readonly string m_AssemblyQualifiedName;

	public string AssemblyQualifiedName {
		get { return m_AssemblyQualifiedName; }
	}


	private System.Type m_SystemType;
	public System.Type SystemType {
		get {
			if (m_SystemType == null) {
				GetSystemType();
			}
			return m_SystemType;
		}
	}

	private void GetSystemType() {
		m_SystemType = System.Type.GetType(m_AssemblyQualifiedName);
	}

	public SerializableType(System.Type _SystemType) {
		m_SystemType = _SystemType;
		m_AssemblyQualifiedName = _SystemType.AssemblyQualifiedName;
	}
}