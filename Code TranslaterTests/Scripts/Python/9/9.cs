class Solution
{
	List<Object> ans = new List<object>();
	
	List<int> getLonelyNodes(TreeNode? root)
	{
		void helper(object root)
		{
			if (root.left == null && root.right)
			{
				this.ans.append(root.right.val);
				return helper(root.right);
			}
			if (root.left && root.right == null)
			{
				this.ans.append(root.left.val);
				return helper(root.left);
			}
			if (root.left && root.right)
			{
				return helper(root.left), helper(root.right);
			}
			if (root.left == null && root.right == null)
			{
				return;
			}
		}
		
		helper(root);
		ans = this.ans;
		this.ans = new List<object>();
		return ans;
	}
}